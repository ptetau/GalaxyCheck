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

        internal int TestCase { get; private set; }

        internal bool IsActive { get; private set; }

        internal int Discards { get; set; }

        internal long Consumption { get; private set; }

        internal int Length => _length;

        internal ulong[] Choices => _choices;

        internal byte[] Frozen => _frozen;

        internal int SpanCount => _spanCount;

        internal int[] Spans => _spans;

        internal int CarryCount => _carryCount;

        internal int[] Carries => _carries;

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
