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
    internal sealed class Shrinker
    {
        private const int MaxCandidates = 20_000;

        private ulong[] _best;
        private byte[] _bestFrozen;
        private int _bestLength;
        private int[] _bestSpans;
        private int _bestSpanCount;
        private int[] _bestCarries;
        private int _bestCarryCount;

        private ulong[] _candidate;
        private int _candidateLength;

        private readonly int _shrinkLimit;

        public Shrinker(GenSource source, int shrinkLimit)
        {
            _shrinkLimit = shrinkLimit;
            _best = new ulong[Math.Max(source.Length, 4)];
            _bestFrozen = new byte[_best.Length];
            _bestSpans = new int[Math.Max(source.SpanCount * 2, 2)];
            _bestCarries = new int[Math.Max(source.CarryCount, 2)];
            _candidate = new ulong[_best.Length];
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

        private bool IsSimpler(ulong[] choices, int length)
        {
            if (length != _bestLength) return length < _bestLength;

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
        }

        private bool IsCarry(int index)
        {
            for (var k = 0; k < _bestCarryCount; k++)
            {
                if (_bestCarries[k] == index) return true;
            }

            return false;
        }

        /// <summary>
        /// Yields once for each candidate. After each yield, the driver runs the candidate and calls TryAccept.
        /// </summary>
        public IEnumerable<bool> Candidates()
        {
            var candidates = 0;
            bool improved;

            if (ReachedShrinkLimit) yield break;

            do
            {
                improved = false;

                // Try the simplest value of each choice, the first choice first. A first choice is often the
                // choice of a branch, such as null or not null.
                for (var i = 0; i < _bestLength; i++)
                {
                    if (_bestFrozen[i] != 0 || _best[i] == 0) continue;

                    var shrinks = Shrinks;
                    SetCandidate(i, 0);
                    yield return true;
                    if (++candidates >= MaxCandidates || ReachedShrinkLimit) yield break;
                    if (Shrinks > shrinks) improved = true;
                }

                // Delete each span, the last first. A span is an element of a collection, or a value that a filter
                // threw away.
                for (var k = _bestSpanCount - 1; k >= 0; k--)
                {
                    if (k >= _bestSpanCount) continue;

                    var start = _bestSpans[k * 2];
                    var end = _bestSpans[k * 2 + 1];
                    if (end <= start || end > _bestLength) continue;

                    var shrinks = Shrinks;
                    _candidateLength = 0;
                    Append(_best, 0, start);
                    Append(_best, end, _bestLength - end);
                    yield return true;
                    if (++candidates >= MaxCandidates || ReachedShrinkLimit) yield break;
                    if (Shrinks > shrinks) improved = true;
                }

                // Make each choice as small as it can be.
                for (var i = 0; i < _bestLength; i++)
                {
                    if (_bestFrozen[i] != 0 || _best[i] == 0) continue;

                    var shrinks = Shrinks;
                    SetCandidate(i, 0);
                    yield return true;
                    if (++candidates >= MaxCandidates || ReachedShrinkLimit) yield break;
                    if (Shrinks > shrinks)
                    {
                        improved = true;
                        continue;
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
                        if (++candidates >= MaxCandidates || ReachedShrinkLimit) yield break;
                        if (Shrinks > shrinks)
                        {
                            improved = true;
                        }
                        else
                        {
                            low = mid;
                        }
                    }

                    // The high part of a number is as small as it can be with the current low part: lower it by one
                    // and raise the low part to its maximum, so that the low part can then shrink.
                    if (i + 1 < _bestLength && _best[i] > 0 && _bestFrozen[i + 1] == 0 && IsCarry(i))
                    {
                        shrinks = Shrinks;
                        SetCandidate(i, _best[i] - 1);
                        _candidate[i + 1] = ulong.MaxValue;
                        yield return true;
                        if (++candidates >= MaxCandidates || ReachedShrinkLimit) yield break;
                        if (Shrinks > shrinks) improved = true;
                    }
                }
            } while (improved);
        }

        private void SetCandidate(int index, ulong value)
        {
            Array.Copy(_best, _candidate, _bestLength);
            _candidate[index] = value;
            _candidateLength = _bestLength;
        }

        private void Append(ulong[] source, int start, int count)
        {
            if (count <= 0) return;
            Array.Copy(source, start, _candidate, _candidateLength, count);
            _candidateLength += count;
        }
    }
}
