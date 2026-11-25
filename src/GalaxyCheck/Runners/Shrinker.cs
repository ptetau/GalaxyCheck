using GalaxyCheck.Gens.Parameters;
using System;
using System.Collections.Generic;

namespace GalaxyCheck.Runners
{
    /// <summary>
    /// Shrinks the choices of a failing test case. The shrinker proposes candidate choices, the driver runs the test
    /// case on each candidate, and the shrinker keeps a candidate when the test fails and its choices are simpler.
    /// Choices are simpler when they are fewer, or when they are as many and smaller in the first place that differs.
    /// </summary>
    /// <remarks>
    /// The shrinker runs its passes to a fixed point. The cheap passes (zero a choice, delete a span, minimize a choice)
    /// run first. When they make no progress, the structural passes run one at a time, and the first that makes
    /// progress hands back to the cheap passes. A span is the choices of one part of a value, for example an element
    /// of a collection, an attempt of a filter, or a branch of a choice.
    /// </remarks>
    [global::Verifiable.Contracts.Unverifiable]
    internal sealed class Shrinker
    {
        private const int MaxCandidates = 20_000;

        /// <summary>The distance between two choices that the passes on pairs of choices try.</summary>
        private const int PairWindow = 8;

        /// <summary>Above this count of spans, the passes that compare each span with each other span do not run.</summary>
        private const int MaxStructureSpans = 512;

        /// <summary>The longest run of sibling spans that the reorder pass swaps pair by pair.</summary>
        private const int MaxSwapRun = 32;

        private ulong[] _best;
        private byte[] _bestFrozen;
        private int _bestLength;
        private int[] _bestSpans;
        private int _bestSpanCount;
        private int[] _bestCarries;
        private int _bestCarryCount;
        private int[] _bestStops;
        private int _bestStopCount;

        private ulong[] _candidate;
        private int _candidateLength;

        // For each choice, the count of shrinks plus one when its zero last failed, so that a pass does not try it again.
        private int[] _zeroFailed;

        // For each choice, one when it is the first choice of a span and is one, as the choice "one more element" of a
        // collection is. The pass that lowers pairs does not lower two of these together.
        private byte[] _markers = Array.Empty<byte>();
        private int _markersVersion = -1;

        private readonly int _shrinkLimit;
        private int _candidates;

        // True in the last stage, which moves content toward the front.
        private bool _normalizing;
        private readonly List<int> _runs = new List<int>();

        // The structure of the best choices: the parent and the next sibling of each span. Built on demand.
        private int[] _parent = Array.Empty<int>();
        private int[] _next = Array.Empty<int>();
        private int[] _order = Array.Empty<int>();
        private int _structureVersion = -1;

        public Shrinker(GenSource source, int shrinkLimit)
        {
            _shrinkLimit = shrinkLimit;
            _best = new ulong[Math.Max(source.Length, 4)];
            _bestFrozen = new byte[_best.Length];
            _bestSpans = new int[Math.Max(source.SpanCount * 2, 2)];
            _bestCarries = new int[Math.Max(source.CarryCount, 2)];
            _bestStops = new int[Math.Max(source.StopCount, 2)];
            _candidate = new ulong[_best.Length];
            _zeroFailed = new int[_best.Length];
            Copy(source);
        }

        public int Shrinks { get; private set; }

        public bool ReachedShrinkLimit => Shrinks >= _shrinkLimit;

        public ulong[] Candidate => _candidate;

        public int CandidateLength => _candidateLength;

        public ulong[] Best => _best;

        public int BestLength => _bestLength;

        /// <summary>
        /// Keeps the choices that the source recorded, if they are simpler than the best choices.
        /// </summary>
        public bool TryAccept(GenSource source)
        {
            if (!IsSimpler(source.Choices, source.Length))
            {
                return false;
            }

            Copy(source);
            Shrinks++;
            return true;
        }

        /// <summary>True when the choices are simpler than the best choices.</summary>
        public bool IsSimpler(ulong[] choices, int length)
        {
            if (length != _bestLength) return length < _bestLength;

            if (_normalizing)
            {
                for (var i = length - 1; i >= 0; i--)
                {
                    if (choices[i] != _best[i]) return choices[i] < _best[i];
                }

                return false;
            }

            for (var i = 0; i < length; i++)
            {
                if (choices[i] != _best[i]) return choices[i] < _best[i];
            }

            return false;
        }

        private void Copy(GenSource source)
        {
            var length = source.Length;
            if (_best.Length < length)
            {
                var capacity = Math.Max(length, _best.Length * 2);
                _best = new ulong[capacity];
                _bestFrozen = new byte[capacity];
                _candidate = new ulong[capacity];
                _zeroFailed = new int[capacity];
            }

            Array.Copy(source.Choices, _best, length);
            Array.Copy(source.Frozen, _bestFrozen, length);
            _bestLength = length;

            var spanInts = source.SpanCount * 2;
            if (_bestSpans.Length < spanInts)
            {
                _bestSpans = new int[Math.Max(spanInts, _bestSpans.Length * 2)];
            }

            Array.Copy(source.Spans, _bestSpans, spanInts);
            _bestSpanCount = source.SpanCount;

            if (_bestCarries.Length < source.CarryCount)
            {
                _bestCarries = new int[Math.Max(source.CarryCount, _bestCarries.Length * 2)];
            }

            Array.Copy(source.Carries, _bestCarries, source.CarryCount);
            _bestCarryCount = source.CarryCount;

            if (_bestStops.Length < source.StopCount)
            {
                _bestStops = new int[Math.Max(source.StopCount, _bestStops.Length * 2)];
            }

            Array.Copy(source.Stops, _bestStops, source.StopCount);
            _bestStopCount = source.StopCount;
        }

        private bool IsCarry(int index)
        {
            for (var k = 0; k < _bestCarryCount; k++)
            {
                if (_bestCarries[k] == index) return true;
            }

            return false;
        }

        private bool IsStop(int index)
        {
            for (var k = 0; k < _bestStopCount; k++)
            {
                if (_bestStops[k] == index) return true;
            }

            return false;
        }

        private int SpanStart(int k) => _bestSpans[k * 2];

        private int SpanEnd(int k) => _bestSpans[k * 2 + 1];

        private bool IsFree(int i) => _bestFrozen[i] == 0;

        private bool IsMarker(int i)
        {
            if (_markersVersion != Shrinks)
            {
                if (_markers.Length < _bestLength) _markers = new byte[Math.Max(_bestLength, _markers.Length * 2)];
                Array.Clear(_markers, 0, _bestLength);
                for (var k = 0; k < _bestSpanCount; k++)
                {
                    var start = SpanStart(k);
                    if (start < _bestLength && _best[start] == 1) _markers[start] = 1;
                }

                _markersVersion = Shrinks;
            }

            return _markers[i] != 0;
        }

        private bool Stop() => ++_candidates >= MaxCandidates || ReachedShrinkLimit;

        /// <summary>
        /// Yields once for each candidate. After each yield, the driver runs the candidate and calls TryAccept.
        /// </summary>
        public IEnumerable<bool> Candidates()
        {
            if (ReachedShrinkLimit) yield break;

            while (true)
            {
                var shrinks = Shrinks;
                foreach (var _ in CheapPasses())
                {
                    yield return true;
                    if (Stop()) yield break;
                }

                if (Shrinks > shrinks) continue;

                var improved = false;
                for (var pass = 0; pass < StructuralPassCount && !improved; pass++)
                {
                    shrinks = Shrinks;
                    foreach (var _ in StructuralPass(pass))
                    {
                        yield return true;
                        if (Stop()) yield break;
                    }

                    improved = Shrinks > shrinks;
                }

                if (improved) continue;

                // The choices are as simple as the passes make them. Put the content of the parts first.
                var length = _bestLength;
                _normalizing = true;
                while (true)
                {
                    shrinks = Shrinks;
                    foreach (var _ in Normalize())
                    {
                        yield return true;
                        if (Stop()) yield break;
                    }

                    if (Shrinks == shrinks || _bestLength < length) break;
                }

                _normalizing = false;
                if (_bestLength == length) yield break;
            }
        }

        /// <summary>
        /// Moves the elements of collections toward the front, when the test still fails with choices of the same
        /// length: the elements of a collection move into an empty collection just before it, and the elements of two
        /// collections of one parent swap. The order inside a collection stays as the other passes set it. While it runs, choices of the same length are simpler when they are
        /// smaller in the last place that differs, so that the simplest parts come last.
        /// </summary>
        private IEnumerable<bool> Normalize()
        {
            if (!BuildStructure()) yield break;

            var runs = _runs;
            runs.Clear();
            for (var m = 0; m < _bestSpanCount; m++)
            {
                // Only the elements of a collection move: a run that the end of its collection follows.
                if (FirstOfRun(m) && IsStop(RunOf(m).End)) runs.Add(m);
            }

            foreach (var first in runs)
            {
                var (runStart, runEnd, _) = RunOf(first);
                var lowest = runStart;
                while (lowest > 0 && IsStop(lowest - 1) && OnlyInAncestors(lowest - 1, runStart, runEnd))
                {
                    lowest--;
                }

                for (var q = lowest; q < runStart; q++)
                {
                    _candidateLength = 0;
                    Append(_best, 0, q);
                    Append(_best, runStart, runEnd - runStart);
                    Append(_best, q, runStart - q);
                    Append(_best, runEnd, _bestLength - runEnd);
                    if (!IsSimpler(_candidate, _candidateLength)) continue;

                    var shrinks = Shrinks;
                    yield return true;
                    if (Shrinks > shrinks) yield break;
                }
            }

            for (var a = 0; a < runs.Count; a++)
            {
                for (var b = 0; b < runs.Count; b++)
                {
                    if (_parent[runs[a]] != _parent[runs[b]]) continue;

                    var (aStart, aEnd, _) = RunOf(runs[a]);
                    var (bStart, bEnd, _) = RunOf(runs[b]);
                    if (aEnd > bStart) continue;

                    _candidateLength = 0;
                    Append(_best, 0, aStart);
                    Append(_best, bStart, bEnd - bStart);
                    Append(_best, aEnd, bStart - aEnd);
                    Append(_best, aStart, aEnd - aStart);
                    Append(_best, bEnd, _bestLength - bEnd);
                    if (!IsSimpler(_candidate, _candidateLength)) continue;

                    var shrinks = Shrinks;
                    yield return true;
                    if (Shrinks > shrinks) yield break;
                }
            }
        }

        /// <summary>True when each span that holds the choice also holds the run.</summary>
        private bool OnlyInAncestors(int index, int runStart, int runEnd)
        {
            for (var k = 0; k < _bestSpanCount; k++)
            {
                var start = SpanStart(k);
                var end = SpanEnd(k);
                if (start <= index && index < end && (start > runStart || end < runEnd)) return false;
            }

            return true;
        }

        private IEnumerable<bool> CheapPasses()
        {
            // Try the simplest value of each choice, the first choice first. A first choice is often the choice of a
            // branch, such as null or not null.
            for (var i = 0; i < _bestLength; i++)
            {
                if (!IsFree(i) || _best[i] == 0) continue;

                var before = Shrinks;
                SetCandidate(i, 0);
                yield return true;
                if (Shrinks == before && i < _zeroFailed.Length) _zeroFailed[i] = Shrinks + 1;
            }

            // Delete each span, the last first. A span is an element of a collection, or a value that a filter threw
            // away.
            for (var k = _bestSpanCount - 1; k >= 0; k--)
            {
                if (k >= _bestSpanCount) continue;

                var start = SpanStart(k);
                var end = SpanEnd(k);
                if (end <= start || end > _bestLength) continue;

                Without(start, end);
                yield return true;
            }

            // Make each choice as small as it can be.
            for (var i = 0; i < _bestLength; i++)
            {
                if (!IsFree(i) || _best[i] == 0) continue;

                var shrinks = Shrinks;
                if (i >= _zeroFailed.Length || _zeroFailed[i] != Shrinks + 1)
                {
                    SetCandidate(i, 0);
                    yield return true;
                    if (Shrinks > shrinks) continue;
                }

                // The best value at i fails and 0 passes: search for the smallest value that fails.
                var low = 0UL;
                while (i < _bestLength && _best[i] > low + 1)
                {
                    var high = _best[i];
                    var mid = low + (high - low) / 2;
                    shrinks = Shrinks;
                    SetCandidate(i, mid);
                    yield return true;
                    if (Shrinks == shrinks) low = mid;
                }

                // The high part of a number is as small as it can be with the current low part: lower it by one and
                // raise the low part to its maximum, so that the low part can then shrink.
                if (i + 1 < _bestLength && _best[i] > 0 && IsFree(i + 1) && IsCarry(i))
                {
                    SetCandidate(i, _best[i] - 1);
                    _candidate[i + 1] = ulong.MaxValue;
                    yield return true;
                }
            }
        }

        private const int StructuralPassCount = 8;

        private IEnumerable<bool> StructuralPass(int pass) => pass switch
        {
            0 => DeleteSpanAndLowerCount(),
            1 => LowerPairsTogether(),
            2 => DeleteIntervals(2),
            3 => ReorderSiblings(),
            4 => ReplaceByDescendant(),
            5 => Redistribute(),
            6 => DeleteSpanAndAdjust(),
            _ => MergeNeighbours(),
        };

        /// <summary>
        /// Deletes a span and lowers a choice just before its run of siblings by one. A collection whose count is a
        /// choice made before its elements, as in a bind, keeps its elements in step with its count.
        /// </summary>
        private IEnumerable<bool> DeleteSpanAndLowerCount()
        {
            if (!BuildStructure()) yield break;

            for (var k = _bestSpanCount - 1; k >= 0; k--)
            {
                if (k >= _bestSpanCount || !BuildStructure()) yield break;

                var start = SpanStart(k);
                var end = SpanEnd(k);
                if (end <= start || end > _bestLength) continue;

                var runStart = start;
                for (var m = 0; m < _bestSpanCount; m++)
                {
                    // The first sibling of the run: follow the siblings back to the first.
                    if (_next[m] >= 0 && FirstOfRun(m) && Reaches(m, k))
                    {
                        runStart = SpanStart(m);
                        break;
                    }
                }

                var shrinks = Shrinks;
                for (var j = runStart - 1; j >= 0 && j >= runStart - 2 && Shrinks == shrinks; j--)
                {
                    if (!IsFree(j) || _best[j] == 0) continue;

                    Without(start, end);
                    _candidate[j] = _best[j] - 1;
                    yield return true;
                }

                if (Shrinks > shrinks) yield break;
            }
        }

        /// <summary>
        /// Deletes a span and lowers by one a choice of at least two, or two near choices, in the run of siblings of the
        /// span. A value that refers to a place in a collection then keeps its place when an element before it goes.
        /// </summary>
        private IEnumerable<bool> DeleteSpanAndAdjust()
        {
            if (!BuildStructure()) yield break;

            for (var k = _bestSpanCount - 1; k >= 0; k--)
            {
                if (k >= _bestSpanCount) yield break;

                var start = SpanStart(k);
                var end = SpanEnd(k);
                if (end <= start || end > _bestLength) continue;

                var (runStart, runEnd, runLength) = RunOf(k);
                var shrinks = Shrinks;

                for (var i = runStart; i < runEnd; i++)
                {
                    if (i >= start && i < end || !IsFree(i) || _best[i] < 2) continue;

                    Without(start, end);
                    _candidate[i < start ? i : i - (end - start)] = _best[i] - 1;
                    yield return true;
                    if (Shrinks > shrinks) yield break;
                }

                if (runLength > MaxSwapRun) continue;

                for (var i = runStart; i < runEnd; i++)
                {
                    if (i >= start && i < end || !IsFree(i) || _best[i] < 2) continue;

                    for (var j = i + 1; j < runEnd && j <= i + PairWindow; j++)
                    {
                        if (j >= start && j < end || !IsFree(j) || _best[j] == 0) continue;

                        Without(start, end);
                        _candidate[i < start ? i : i - (end - start)] = _best[i] - 1;
                        _candidate[j < start ? j : j - (end - start)] = _best[j] - 1;
                        yield return true;
                        if (Shrinks > shrinks) yield break;
                    }
                }
            }
        }

        /// <summary>
        /// Deletes a span and moves its amount onto the same choice of the next or the previous sibling of equal length,
        /// so that two elements become one. The amount adds or subtracts, and the binary choice after it, such as a
        /// side, may flip: two values then join into their sum, also across the sign.
        /// </summary>
        private IEnumerable<bool> MergeNeighbours()
        {
            if (!BuildStructure()) yield break;

            for (var k = _bestSpanCount - 1; k >= 0; k--)
            {
                if (k >= _bestSpanCount || !BuildStructure()) yield break;

                var m = _next[k];
                if (m < 0) continue;

                var length = SpanEnd(k) - SpanStart(k);
                if (length != SpanEnd(m) - SpanStart(m)) continue;

                for (var direction = 0; direction < 2; direction++)
                {
                    // The deleted span and the span that takes its amount.
                    var deleted = direction == 0 ? k : m;
                    var kept = direction == 0 ? m : k;
                    var start = SpanStart(deleted);
                    var end = SpanEnd(deleted);
                    var keptStart = SpanStart(kept);
                    var shift = keptStart > start ? end - start : 0;

                    for (var o = 0; o < length; o++)
                    {
                        var from = start + o;
                        var to = keptStart + o;
                        if (_best[from] == 0 || _best[from] <= 1 && _best[to] <= 1 || !IsFree(from) || !IsFree(to)) continue;

                        var flips = o + 1 < length && IsFree(to + 1) && _best[to + 1] <= 1 && _best[from + 1] <= 1;
                        for (var variant = 0; variant < 3; variant++)
                        {
                            // The sum, the sum across the sign, and the difference on the side of the larger.
                            var flip = variant == 1 || variant == 2 && _best[from] > _best[to];
                            if (flip && !flips) continue;

                            ulong value;
                            if (variant < 2)
                            {
                                if (_best[to] > ulong.MaxValue - _best[from]) continue;
                                value = _best[to] + _best[from];
                            }
                            else
                            {
                                value = _best[to] > _best[from] ? _best[to] - _best[from] : _best[from] - _best[to];
                            }

                            var shrinks = Shrinks;
                            Without(start, end);
                            _candidate[to - shift] = value;
                            if (flip) _candidate[to + 1 - shift] = 1 - _best[to + 1];
                            yield return true;
                            if (Shrinks > shrinks) yield break;
                        }
                    }
                }
            }
        }

        /// <summary>The choices of the run of siblings of a span, and the count of its spans.</summary>
        private (int Start, int End, int Count) RunOf(int k)
        {
            for (var m = 0; m < _bestSpanCount; m++)
            {
                if (!FirstOfRun(m) || !Reaches(m, k)) continue;

                var count = 1;
                var last = m;
                while (_next[last] >= 0 && count <= _bestSpanCount)
                {
                    last = _next[last];
                    count++;
                }

                return (SpanStart(m), SpanEnd(last), count);
            }

            return (SpanStart(k), SpanEnd(k), 1);
        }

        private bool FirstOfRun(int m)
        {
            for (var p = 0; p < _bestSpanCount; p++)
            {
                if (_next[p] == m) return false;
            }

            return true;
        }

        private bool Reaches(int from, int to)
        {
            for (var steps = 0; from >= 0 && steps <= _bestSpanCount; steps++)
            {
                if (from == to) return true;
                from = _next[from];
            }

            return false;
        }

        /// <summary>
        /// Lowers two near choices by the same amount, so that their difference stays the same, for example two equal
        /// values or two values that must differ by one.
        /// </summary>
        private IEnumerable<bool> LowerPairsTogether()
        {
            for (var i = 0; i < _bestLength; i++)
            {
                for (var j = i + 1; j < _bestLength && j <= i + PairWindow; j++)
                {
                    if (!IsFree(i) || _best[i] == 0) break;
                    if (!IsFree(j) || _best[j] == 0 || IsMarker(i) && IsMarker(j)) continue;

                    var most = Math.Min(_best[i], _best[j]);
                    var shrinks = Shrinks;
                    SetCandidate(i, _best[i] - most);
                    _candidate[j] = _best[j] - most;
                    yield return true;
                    if (Shrinks > shrinks || most == 1) continue;

                    SetCandidate(i, _best[i] - 1);
                    _candidate[j] = _best[j] - 1;
                    yield return true;
                    if (Shrinks == shrinks) continue;

                    // Lowering both by one fails and by the most passes: search for the largest step that fails.
                    var low = 0UL;
                    var high = most - 1;
                    while (high > low + 1 && i < _bestLength && j < _bestLength)
                    {
                        var mid = low + (high - low) / 2;
                        shrinks = Shrinks;
                        SetCandidate(i, _best[i] - mid);
                        _candidate[j] = _best[j] - mid;
                        yield return true;
                        if (Shrinks > shrinks)
                        {
                            high -= mid;
                            low = 0;
                        }
                        else
                        {
                            high = mid;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Moves an amount from a choice to a later choice, so that their sum stays the same and the earlier choice is
        /// smaller.
        /// </summary>
        private IEnumerable<bool> Redistribute()
        {
            for (var i = 0; i < _bestLength; i++)
            {
                for (var j = i + 1; j < _bestLength && j <= i + PairWindow; j++)
                {
                    if (!IsFree(i) || _best[i] < 1) break;
                    if (!IsFree(j)) continue;

                    var amount = _best[i];
                    if (_best[j] > ulong.MaxValue - amount) continue;

                    var shrinks = Shrinks;
                    SetCandidate(i, 0);
                    _candidate[j] = _best[j] + amount;
                    yield return true;
                    if (Shrinks > shrinks || amount == 1) continue;

                    SetCandidate(i, _best[i] - 1);
                    _candidate[j] = _best[j] + 1;
                    yield return true;
                    if (Shrinks == shrinks) continue;

                    // Moving one passes the test and moving all fails it: search for the largest amount that fails.
                    var low = 0UL;
                    var high = amount - 1;
                    while (high > low + 1 && i < _bestLength && j < _bestLength)
                    {
                        var mid = low + (high - low) / 2;
                        if (_best[i] < mid || _best[j] > ulong.MaxValue - mid) break;

                        shrinks = Shrinks;
                        SetCandidate(i, _best[i] - mid);
                        _candidate[j] = _best[j] + mid;
                        yield return true;
                        if (Shrinks > shrinks)
                        {
                            high -= mid;
                            low = 0;
                        }
                        else
                        {
                            high = mid;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deletes each run of the given number of choices that ends with the first choice of a span, the last first.
        /// This joins two collections into one, for example, when it deletes the end of one and the start of the next.
        /// </summary>
        private IEnumerable<bool> DeleteIntervals(int width)
        {
            for (var i = _bestLength - width; i >= 0; i--)
            {
                if (i + width > _bestLength) continue;

                if (!StartsSpan(i + width - 1)) continue;

                var free = true;
                for (var j = i; j < i + width; j++)
                {
                    if (!IsFree(j)) free = false;
                }

                if (!free) continue;

                Without(i, i + width);
                yield return true;
            }
        }

        /// <summary>
        /// Puts each run of sibling spans in order, the simplest first: first the whole run, then each pair of
        /// neighbours.
        /// </summary>
        private IEnumerable<bool> ReorderSiblings()
        {
            if (!BuildStructure()) yield break;

            var runs = new List<int>();
            for (var m = 0; m < _bestSpanCount; m++)
            {
                if (_next[m] >= 0 && FirstOfRun(m)) runs.Add(m);
            }

            var spans = new List<int>();
            foreach (var first in runs)
            {
                if (!BuildStructure()) yield break;

                // The spans of a run do not change while no candidate is kept.
                var shrinks = Shrinks;
                spans.Clear();
                for (var m = first; m >= 0 && spans.Count <= _bestSpanCount; m = _next[m])
                {
                    spans.Add(m);
                }

                var sorted = spans.ToArray();
                Array.Sort(sorted, CompareSpans);
                var changed = false;
                for (var s = 0; s < sorted.Length; s++)
                {
                    if (CompareSpans(sorted[s], spans[s]) != 0) changed = true;
                }

                if (!changed) continue;

                _candidateLength = 0;
                Append(_best, 0, SpanStart(spans[0]));
                foreach (var m in sorted)
                {
                    Append(_best, SpanStart(m), SpanEnd(m) - SpanStart(m));
                }

                var runEnd = SpanEnd(spans[spans.Count - 1]);
                Append(_best, runEnd, _bestLength - runEnd);
                yield return true;
                if (Shrinks > shrinks) yield break;
                if (spans.Count > MaxSwapRun) continue;

                for (var s = 0; s + 1 < spans.Count; s++)
                {
                    var a = spans[s];
                    var b = spans[s + 1];
                    if (CompareSpans(a, b) <= 0) continue;

                    _candidateLength = 0;
                    Append(_best, 0, SpanStart(a));
                    Append(_best, SpanStart(b), SpanEnd(b) - SpanStart(b));
                    Append(_best, SpanStart(a), SpanEnd(a) - SpanStart(a));
                    Append(_best, SpanEnd(b), _bestLength - SpanEnd(b));
                    yield return true;
                    if (Shrinks > shrinks) yield break;
                }
            }
        }

        private bool StartsSpan(int index)
        {
            for (var k = 0; k < _bestSpanCount; k++)
            {
                if (SpanStart(k) == index) return true;
            }

            return false;
        }

        private int CompareSpans(int a, int b)
        {
            var aStart = SpanStart(a);
            var bStart = SpanStart(b);
            var aLength = SpanEnd(a) - aStart;
            var bLength = SpanEnd(b) - bStart;
            if (aLength != bLength) return aLength.CompareTo(bLength);

            for (var i = 0; i < aLength; i++)
            {
                var c = _best[aStart + i].CompareTo(_best[bStart + i]);
                if (c != 0) return c;
            }

            return 0;
        }

        /// <summary>
        /// Replaces a span by a smaller span inside it, for example a tree by one of its subtrees.
        /// </summary>
        private IEnumerable<bool> ReplaceByDescendant()
        {
            if (!BuildStructure()) yield break;

            // The longest spans first, so that the biggest parts go first.
            var count = _bestSpanCount;
            if (_order.Length < count) _order = new int[Math.Max(count, _order.Length * 2)];
            for (var k = 0; k < count; k++) _order[k] = k;
            Array.Sort(_order, 0, count, Comparer<int>.Create((a, b) =>
                (SpanEnd(b) - SpanStart(b)).CompareTo(SpanEnd(a) - SpanStart(a))));

            var order = new int[count];
            Array.Copy(_order, order, count);
            foreach (var p in order)
            {
                var start = SpanStart(p);
                var end = SpanEnd(p);
                for (var c = 0; c < count; c++)
                {
                    var childStart = SpanStart(c);
                    var childEnd = SpanEnd(c);
                    if (childStart < start || childEnd > end || childEnd - childStart >= end - start || childEnd <= childStart)
                    {
                        continue;
                    }

                    var shrinks = Shrinks;
                    _candidateLength = 0;
                    Append(_best, 0, start);
                    Append(_best, childStart, childEnd - childStart);
                    Append(_best, end, _bestLength - end);
                    yield return true;
                    if (Shrinks > shrinks) yield break;
                }
            }
        }

        /// <summary>
        /// Finds the parent and the next sibling of each span of the best choices. Returns false when there are too
        /// many spans.
        /// </summary>
        private bool BuildStructure()
        {
            if (_bestSpanCount > MaxStructureSpans) return false;
            if (_structureVersion == Shrinks) return true;

            var count = _bestSpanCount;
            if (_parent.Length < count)
            {
                _parent = new int[Math.Max(count, _parent.Length * 2)];
                _next = new int[_parent.Length];
            }

            for (var k = 0; k < count; k++)
            {
                var start = SpanStart(k);
                var end = SpanEnd(k);
                var parent = -1;
                for (var m = 0; m < count; m++)
                {
                    if (m == k) continue;
                    var mStart = SpanStart(m);
                    var mEnd = SpanEnd(m);
                    if (mStart > start || mEnd < end || (mStart == start && mEnd == end && m < k)) continue;
                    if (parent < 0 || mEnd - mStart < SpanEnd(parent) - SpanStart(parent)) parent = m;
                }

                _parent[k] = parent;
            }

            for (var k = 0; k < count; k++)
            {
                var end = SpanEnd(k);
                var limit = _parent[k] >= 0 ? SpanEnd(_parent[k]) : int.MaxValue;
                var next = -1;
                for (var m = 0; m < count; m++)
                {
                    if (SpanStart(m) != end || SpanEnd(m) > limit || SpanEnd(m) <= end) continue;
                    if (next < 0 || SpanEnd(m) > SpanEnd(next)) next = m;
                }

                _next[k] = next;
            }

            _structureVersion = Shrinks;
            return true;
        }

        private void SetCandidate(int index, ulong value)
        {
            Array.Copy(_best, _candidate, _bestLength);
            _candidate[index] = value;
            _candidateLength = _bestLength;
        }

        private void Without(int start, int end)
        {
            _candidateLength = 0;
            Append(_best, 0, start);
            Append(_best, end, _bestLength - end);
        }

        private void Append(ulong[] source, int start, int count)
        {
            if (count <= 0) return;
            Array.Copy(source, start, _candidate, _candidateLength, count);
            _candidateLength += count;
        }
    }
}
