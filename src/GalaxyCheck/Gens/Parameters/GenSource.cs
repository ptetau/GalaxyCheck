using GalaxyCheck.Gens.Internal;
using System;
using System.Runtime.CompilerServices;

namespace GalaxyCheck.Gens.Parameters
{
    /// <summary>
    /// The source of the choices that a generator makes. A generator reads each choice as an integer from zero to a
    /// maximum, where zero is the simplest choice. In random mode, the source draws each choice from a random number
    /// generator and records it. In replay mode, the source reads each choice from a recorded sequence, so that the
    /// same generator produces the same value. Shrinking works on the recorded sequence.
    /// </summary>
    [global::Verifiable.Contracts.Unverifiable]
    public sealed class GenSource
    {
        private const ulong Gamma = 0x9E3779B97F4A7C15UL;

        private ulong _rng;
        private ulong? _waypoint;

        private ulong[] _replay = Array.Empty<ulong>();
        private int _replayLength;
        private int _replayPosition;

        private ulong[] _choices;
        private byte[] _frozen;
        private int _length;

        private int[] _spans = new int[8];
        private int _spanCount;

        private int[] _carries = new int[4];
        private int _carryCount;

        private int[] _stops = new int[4];
        private int _stopCount;

        internal GenSource(ulong seed, int size, int capacity = 64)
        {
            _rng = seed;
            Size = size;
            _choices = new ulong[capacity];
            _frozen = new byte[capacity];
        }

        internal static GenSource Create(int seed, int size) => new GenSource(SeedToState(seed), size);

        internal static ulong SeedToState(int seed) => Mix((ulong)(uint)seed * 0x9E3779B97F4A7C15UL + 0x632BE59BD9B4E019UL);

        /// <summary>The size of the current test case, from 0 to 100.</summary>
        public int Size { get; internal set; }

        internal bool IsReplaying { get; private set; }

        /// <summary>
        /// The shrinker of the run, once a test case failed. A candidate whose choices are not simpler than the best
        /// choices does not run the test function, so that each failing input of the test function is a kept shrink.
        /// </summary>
        internal Runners.Shrinker? Shrinker { get; set; }

        /// <summary>True when the test function runs on the value that the source just made.</summary>
        internal bool ShouldRunTest() => Shrinker == null || Shrinker.IsSimpler(_choices, _length);

        /// <summary>True in an exhaustive run, which replays the choices of each value of a finite domain.</summary>
        internal bool IsWalking { get; set; }

        internal int TestCase { get; private set; }

        internal bool IsActive { get; private set; }

        /// <summary>True in a replay that has read each recorded choice. Each later choice is zero.</summary>
        internal bool ReplayExhausted => IsReplaying && _replayPosition >= _replayLength;

        internal int Discards { get; set; }

        /// <summary>
        /// The bounds (1 for the low bound, 2 for the high bound) that the biased integers drew as an extreme value
        /// since a filter last cleared it. A filter reads it to learn which bounds a rejected value held.
        /// </summary>
        internal int ExtremesMade { get; set; }

        /// <summary>
        /// The bounds that the biased integers do not draw as an extreme value, in the retries of a filter that
        /// rejected them. A bound is one fixed value, so a retry that draws it again is wasted.
        /// </summary>
        internal int AvoidExtremes { get; set; }

        private System.Collections.Generic.Dictionary<object, int>? _rejectedBounds;

        /// <summary>The bounds that a filter rejected and has not kept since, in the earlier values of the run.</summary>
        internal int RejectedBounds(object filter) =>
            _rejectedBounds != null && _rejectedBounds.TryGetValue(filter, out var bounds) ? bounds : 0;

        /// <summary>
        /// Learns the bounds that a filter rejects, for the retries of its later values. A kept bound is forgotten,
        /// so a filter that keeps a bound draws it again. The key is the same as for <see cref="RememberGoodSize"/>.
        /// </summary>
        internal void LearnBounds(object filter, int rejected, int kept)
        {
            if (IsReplaying || (rejected == 0 && _rejectedBounds == null)) return;
            _rejectedBounds ??= new System.Collections.Generic.Dictionary<object, int>();
            _rejectedBounds[filter] = (RejectedBounds(filter) | rejected) & ~kept;
        }

        /// <summary>The values that a filter kept. With the discards, it gives the share that the filters discard.</summary>
        internal int Kept { get; set; }

        /// <summary>
        /// True when the user fixed the size. A filter then retries at that size, and leaves it only when it cannot pass
        /// at that size.
        /// </summary>
        internal bool SizeFixed { get; set; }

        private System.Collections.Generic.Dictionary<object, int>? _goodSizes;

        /// <summary>
        /// The retries of a filter at a fixed size before the filter searches other sizes. Only a filter that cannot
        /// pass at the fixed size, such as a filter of non-zero integers at size 0, leaves that size.
        /// </summary>
        private const int FixedSizeRetries = 50;

        /// <summary>
        /// The size of a retry of a filter after a discard. The size is a frozen choice, so that a replay reads the
        /// size of each retry from the recorded choices.
        /// </summary>
        internal int RetrySize(object filter, int baseSize, int retry)
        {
            if (IsReplaying)
            {
                var replayed = (int)Read(100);
                FreezeLast();
                return replayed;
            }

            var size =
                !SizeFixed ? SizeSearch.Next(baseSize, retry, GoodSize(filter)) :
                retry < FixedSizeRetries ? baseSize :
                SizeSearch.Next(baseSize, retry - FixedSizeRetries + 1, GoodSize(filter));
            Record((ulong)size);
            FreezeLast();
            return size;
        }

        /// <summary>
        /// Remembers the size of a retry where a filter kept a value, for the retries of later discards. The key is the
        /// same for each object of one filter, for example the method of its predicate, and it holds no closure.
        /// </summary>
        internal void RememberGoodSize(object filter, int size)
        {
            if (IsReplaying) return;
            _goodSizes ??= new System.Collections.Generic.Dictionary<object, int>();
            _goodSizes[filter] = SizeSearch.Learn(GoodSize(filter), size);
        }

        private int GoodSize(object filter) =>
            _goodSizes != null && _goodSizes.TryGetValue(filter, out var size) ? size : SizeSearch.NoGoodSize;

        internal long Consumption { get; private set; }

        internal int Length => _length;

        internal ulong[] Choices => _choices;

        internal byte[] Frozen => _frozen;

        internal int SpanCount => _spanCount;

        internal int[] Spans => _spans;

        internal int CarryCount => _carryCount;

        internal int[] Carries => _carries;

        internal int StopCount => _stopCount;

        internal int[] Stops => _stops;

        internal ulong RngState => _rng;

        internal void BeginRandom(int size)
        {
            IsReplaying = false;
            Begin(size);
        }

        internal void BeginReplay(ulong[] choices, int length, int size)
        {
            IsReplaying = true;
            _replay = choices;
            _replayLength = length;
            _replayPosition = 0;
            Begin(size);
        }

        private void Begin(int size)
        {
            Size = size;
            _length = 0;
            _spanCount = 0;
            _carryCount = 0;
            _stopCount = 0;
            _waypoint = null;
            TestCase++;
            IsActive = true;
        }

        internal void End()
        {
            IsActive = false;
        }

        /// <summary>Draws a uniform choice from 0 to max.</summary>
        internal ulong DrawUniform(ulong max) => IsReplaying ? Read(max) : Record(NextBounded(max));

        /// <summary>Reads the next recorded choice. A missing choice is zero, and a choice above max is max.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ulong Read(ulong max)
        {
            var value = _replayPosition < _replayLength ? _replay[_replayPosition++] : 0UL;
            return Record(value > max ? max : value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ulong Record(ulong value)
        {
            if (_length == _choices.Length)
            {
                Grow();
            }

            _frozen[_length] = 0;
            _choices[_length++] = value;
            if (!IsReplaying)
            {
                Consumption++;
            }

            return value;
        }

        /// <summary>Replaces a recorded choice, when the value that it made has a simpler choice.</summary>
        internal void Rewrite(int index, ulong value)
        {
            _choices[index] = value;
        }

        /// <summary>Marks the last recorded choice, so that the shrinker does not change it.</summary>
        internal void FreezeLast()
        {
            if (_length > 0)
            {
                _frozen[_length - 1] = 1;
            }
        }

        private void Grow()
        {
            if (_choices.Length >= 1 << 22)
            {
                throw new Exceptions.GenLimitExceededException(
                    "The generator made too many choices for one value. This is a built-in safety mechanism to prevent hanging tests.");
            }

            Array.Resize(ref _choices, _choices.Length * 2);
            Array.Resize(ref _frozen, _frozen.Length * 2);
        }

        /// <summary>
        /// Marks the last recorded choice as the high part of a number whose low part is the next choice. The
        /// shrinker may lower the high part by one and raise the low part to its maximum.
        /// </summary>
        internal void MarkCarry()
        {
            if (_length == 0) return;

            if (_carryCount == _carries.Length)
            {
                Array.Resize(ref _carries, _carries.Length * 2);
            }

            _carries[_carryCount++] = _length - 1;
        }

        /// <summary>
        /// Marks the last recorded choice as the choice that ends a collection. The shrinker may move the elements of a
        /// later collection before the end of an empty one.
        /// </summary>
        internal void MarkStop()
        {
            if (_length == 0) return;

            if (_stopCount == _stops.Length)
            {
                Array.Resize(ref _stops, _stops.Length * 2);
            }

            _stops[_stopCount++] = _length - 1;
        }

        internal int BeginSpan() => _length;

        internal void EndSpan(int start)
        {
            if (_length <= start) return;

            if (_spanCount * 2 + 2 > _spans.Length)
            {
                Array.Resize(ref _spans, _spans.Length * 2);
            }

            _spans[_spanCount * 2] = start;
            _spans[_spanCount * 2 + 1] = _length;
            _spanCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ulong NextRandom()
        {
            return Mix(_rng += Gamma);
        }

        /// <summary>A uniform random integer from 0 to max, both inclusive.</summary>
        internal ulong NextBounded(ulong max)
        {
            if (max == ulong.MaxValue) return NextRandom();
            if (max == 0) return 0;

            var range = max + 1;
            var high = Math.BigMul(NextRandom(), range, out var low);
            if (low < range)
            {
                var threshold = (0 - range) % range;
                while (low < threshold)
                {
                    high = Math.BigMul(NextRandom(), range, out low);
                }
            }

            return high;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong Mix(ulong z)
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        internal ulong? SetWaypoint()
        {
            var previous = _waypoint;
            _waypoint = _rng;
            return previous;
        }

        internal void RestoreWaypoint(ulong? waypoint)
        {
            _waypoint = waypoint;
        }

        /// <summary>
        /// Moves the random number generator to a state that only depends on the waypoint and the influence, so that
        /// a value does not depend on the values that other generators drew before it. Returns the state to restore.
        /// </summary>
        internal ulong? EnterWaypoint(int influence)
        {
            if (_waypoint is null) return null;

            var saved = _rng;
            _rng = Mix(_waypoint.Value ^ Mix((ulong)(uint)influence + Gamma));
            return saved;
        }

        internal void ExitWaypoint(ulong? saved)
        {
            if (saved is null) return;
            _rng = saved.Value;
        }
    }
}
