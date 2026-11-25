using Verifiable.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;

namespace GalaxyCheck.Properties
{
    /// <summary>
    /// Thrown by <c>Property.Precondition(false)</c>. The runner discards the test case.
    /// </summary>
    [Unverifiable]
    internal sealed class PreconditionFailedException : Exception
    {
        public static readonly PreconditionFailedException Instance = new PreconditionFailedException();

        private PreconditionFailedException() : base("A precondition of the property was false.")
        {
        }
    }

    /// <summary>
    /// The exception of a test case that ran over the time limit.
    /// </summary>
    [Unverifiable]
    internal sealed class TestTimeoutException : TimeoutException
    {
        public TestTimeoutException(long milliseconds) : base($"Timed out after {milliseconds} ms")
        {
        }
    }

    /// <summary>
    /// The token of the test case that runs now. The runner sets it on the thread that starts the test function.
    /// </summary>
    [Unverifiable]
    internal static class TestContext
    {
        [ThreadStatic]
        private static CancellationToken _token;

        public static CancellationToken Token
        {
            get => _token;
            set => _token = value;
        }
    }

    internal sealed record LabelRule<T>(string Label, double? MinPercent, Func<T, bool> When);

    /// <summary>
    /// A generator that has a known number of different values, and knows the choices of each.
    /// </summary>
    internal interface IFiniteGen
    {
        /// <summary>The number of different values, or null when it is more than the limit or not known.</summary>
        long? Count(long limit);

        /// <summary>Appends the choices that make the value with the given index.</summary>
        [Unverifiable]
        void WriteChoices(long index, List<ulong> choices);
    }

    [Verifiable]
    internal static class FiniteMath
    {
        public static bool IsWithinBounds(long min, long max, long origin) =>
            min <= max && origin >= min && origin <= max;

        public static long? CountBoundedInt(long min, long max, long origin, long limit)
        {
            Spec.Requires(limit > 0, "Exhaustive limit must be positive.");
            Spec.Ensures<long?>(r => r == null || (r.Value >= 1 && r.Value <= limit), "Bounded integer count is either null (exceeds limit) or in [1, limit].");

            if (min > max || origin < min || origin > max)
            {
                return null;
            }

            // Guard signed overflow before subtraction: if min < 0 and max >= 0 and max - min could exceed long.MaxValue.
            if (min < 0 && max >= 0 && max > (long.MaxValue + min))
            {
                return null;
            }

            var span = max - min;
            return span < limit ? span + 1 : null;
        }
    }

    [Unverifiable]
    internal static class Finite
    {
        public static long? Of(IGen gen, long limit) => gen.Advanced is IFiniteGen finite ? finite.Count(limit) : null;

        public static long? CountInt(long min, long max, long origin, long limit) =>
            FiniteMath.CountBoundedInt(min, max, origin, limit);

        public static void WriteInt(long min, long max, long origin, long index, List<ulong> choices)
        {
            var up = unchecked((ulong)max - (ulong)origin);
            var down = unchecked((ulong)origin - (ulong)min);
            if (up == 0 && down == 0) return;

            var value = unchecked(min + index);
            var isDown = value < origin;
            var distance = isDown ? unchecked((ulong)origin - (ulong)value) : unchecked((ulong)value - (ulong)origin);
            choices.Add(distance);
            if (up != 0 && down != 0)
            {
                choices.Add(isDown ? 1UL : 0UL);
            }
        }

        public static long? Product(IFiniteGen?[] parts, long limit)
        {
            long total = 1;
            foreach (var part in parts)
            {
                if (part == null) return null;
                var count = part.Count(limit);
                if (count == null) return null;
                if (count.Value > limit / total) return null;
                total *= count.Value;
            }

            return total;
        }

        public static void WriteProduct(IFiniteGen[] parts, long index, List<ulong> choices)
        {
            var indexes = new long[parts.Length];
            for (var i = parts.Length - 1; i >= 0; i--)
            {
                var count = parts[i].Count(long.MaxValue)!.Value;
                indexes[i] = index % count;
                index /= count;
            }

            for (var i = 0; i < parts.Length; i++)
            {
                parts[i].WriteChoices(indexes[i], choices);
            }
        }
    }

    /// <summary>
    /// The count and the choices of a bind (SelectMany) of a small finite generator. The value of the source picks the
    /// generator that follows, so the walk generates each source value from its choices.
    /// </summary>
    [Unverifiable]
    internal static class FiniteBind
    {
        private const long SourceLimit = 1000;

        private static IEnumerable<(long Count, IFiniteGen Inner, ulong[] SourceChoices)> Branches(
            IGen source,
            Func<object?, IGen> selector,
            long limit)
        {
            if (source.Advanced is not IFiniteGen finiteSource) yield break;

            var count = finiteSource.Count(SourceLimit);
            if (count == null) yield break;

            var scratch = new Gens.Parameters.GenSource(0, 100);
            var buffer = new List<ulong>();
            for (long i = 0; i < count.Value; i++)
            {
                buffer.Clear();
                finiteSource.WriteChoices(i, buffer);
                var choices = buffer.ToArray();

                IFiniteGen? inner;
                long? innerCount;
                try
                {
                    scratch.BeginReplay(choices, choices.Length, 100);
                    var value = source.Advanced.GenerateObject(scratch);
                    inner = selector(value).Advanced as IFiniteGen;
                    innerCount = inner?.Count(limit);
                }
                catch (Exception)
                {
                    yield break;
                }
                finally
                {
                    scratch.End();
                }

                if (inner == null || innerCount == null) yield break;
                yield return (innerCount.Value, inner, choices);
            }
        }

        [ThreadStatic]
        private static int _depth;

        public static long? Count(IGen source, Func<object?, IGen> selector, long limit)
        {
            // A generator that refers to itself has no end to count.
            if (_depth >= 6) return null;
            _depth++;
            try
            {
                return CountBranches(source, selector, limit);
            }
            finally
            {
                _depth--;
            }
        }

        private static long? CountBranches(IGen source, Func<object?, IGen> selector, long limit)
        {
            long total = 0;
            var branches = 0L;
            foreach (var branch in Branches(source, selector, limit))
            {
                total += branch.Count;
                branches++;
                if (total > limit) return null;
            }

            var sourceCount = (source.Advanced as IFiniteGen)?.Count(SourceLimit);
            return sourceCount != null && branches == sourceCount.Value && total > 0 ? total : null;
        }

        public static void WriteChoices(IGen source, Func<object?, IGen> selector, long index, List<ulong> choices)
        {
            foreach (var branch in Branches(source, selector, long.MaxValue))
            {
                if (index < branch.Count)
                {
                    choices.AddRange(branch.SourceChoices);
                    branch.Inner.WriteChoices(index, choices);
                    return;
                }

                index -= branch.Count;
            }
        }
    }

    /// <summary>
    /// The options of one check that the runner reads. The default value switches each feature off.
    /// </summary>
    [Verifiable]
    internal static class RunTimeoutMath
    {
        public static Result<long, bool> ResolveMilliseconds(bool hasTimeout, long totalMilliseconds)
        {
            Spec.Ensures<Result<long, bool>>(
                r => !hasTimeout
                    ? (r.IsSuccess && r.Value == 0)
                    : (totalMilliseconds <= 0 ? !r.IsSuccess : (r.IsSuccess && r.Value == totalMilliseconds && r.Value >= 1)),
                "Unset timeout yields 0ms; non-positive timeout fails; positive timeout preserves milliseconds >= 1.");

            if (!hasTimeout)
            {
                return Result<long, bool>.Ok(0);
            }

            if (totalMilliseconds <= 0)
            {
                return Result<long, bool>.Fail(false);
            }

            return Result<long, bool>.Ok(totalMilliseconds);
        }
    }

    [Unverifiable]
    internal readonly struct RunOptions
    {
        public RunOptions(TimeSpan? timeout, bool exhaustive, HealthCheck[]? suppressed)
        {
            var resolved = RunTimeoutMath.ResolveMilliseconds(timeout != null, timeout == null ? 0 : (long)timeout.Value.TotalMilliseconds);
            if (timeout != null && timeout.Value <= TimeSpan.Zero)
            {
                throw new Exceptions.GenErrorException("Error: 'timeout' must be greater than zero.", null);
            }

            TimeoutMilliseconds = timeout == null ? 0 : Math.Max(1L, resolved.IsSuccess ? resolved.Value : 1L);
            Exhaustive = exhaustive;
            HealthChecks = true;

            var mask = 0;
            if (suppressed != null)
            {
                foreach (var check in suppressed)
                {
                    mask |= 1 << (int)check;
                }
            }

            Suppressed = mask;
        }

        public long TimeoutMilliseconds { get; }

        public bool Exhaustive { get; }

        public bool HealthChecks { get; }

        public int Suppressed { get; }

        public bool IsSuppressed(HealthCheck check) => (Suppressed & (1 << (int)check)) != 0;

        public bool Watches(HealthCheck check) => HealthChecks && !IsSuppressed(check);
    }
}
