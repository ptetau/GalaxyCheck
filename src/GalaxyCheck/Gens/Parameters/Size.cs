using System;
using Verifiable.Contracts;

namespace GalaxyCheck.Gens.Parameters
{
    /// <summary>Refined size value constrained to [0, 100].</summary>
    [TypeAlias<int>("Test case size must stay between 0 and 100 so generator magnitude scaling remains bounded.")]
    public readonly partial record struct BoundedSize
    {
        /// <summary>Returns true when <paramref name="value"/> is in [0, 100].</summary>
        public static bool IsValid(int value) => value >= 0 && value <= 100;
    }

    [Verifiable]
    internal static class SizeMath
    {
        public static int NextIncrement(int value)
        {
            Spec.Requires(value >= 0 && value <= 100, "Size must be within 0..100 before incrementing.");
            Spec.Ensures<int>(r => r >= 0 && r < 100, "Incremented size wraps within 0..99.");
            return (value + 1) % 100;
        }

        public static int NextBigIncrement(int value)
        {
            Spec.Requires(value >= 0 && value <= 100, "Size must be within 0..100 before big-incrementing.");
            Spec.Ensures<int>(r => r >= 0 && r < 100, "Big-incremented size wraps within 0..99.");
            return (value + 5) % 100;
        }
    }

    /// <summary>The size of a test case, from 0 to 100. A larger size gives larger values.</summary>
    [Unverifiable]
    public record Size
    {
        /// <summary>The smallest size, 0.</summary>
        public static Size Zero { get; } = new Size(0);
        /// <summary>The largest size, 100.</summary>
        public static Size Max { get; } = new Size(100);

        /// <summary>Creates a size from a verified <see cref="BoundedSize"/>.</summary>
        [Verifiable]
        public static Size From(BoundedSize value) => new Size(value, true);

        private Size(BoundedSize value, bool _)
        {
            Value = value;
        }

        /// <summary>Creates a size from 0 to 100.</summary>
        public Size(int value)
        {
            if (!BoundedSize.IsValid(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "'value' must be between 0 and 100");
            }

            Value = value;
        }

        /// <summary>The size, from 0 to 100.</summary>
        public int Value { get; init; }

        /// <summary>The verified invariant that <see cref="Value"/> stays in [0, 100].</summary>
        [Invariant("Test case size must remain between 0 and 100.")]
        public bool IsWithinBounds() => Value >= 0 && Value <= 100;

        /// <summary>Returns the next size, which wraps from 99 to 0.</summary>
        [Verifiable]
        public Size Increment()
        {
            Spec.Requires(Value >= 0 && Value <= 100, "Current size is within 0..100.");
            return From(BoundedSize.From(SizeMath.NextIncrement(Value)));
        }

        /// <summary>Returns the size five steps up, which wraps at 100.</summary>
        [Verifiable]
        public Size BigIncrement()
        {
            Spec.Requires(Value >= 0 && Value <= 100, "Current size is within 0..100.");
            return From(BoundedSize.From(SizeMath.NextBigIncrement(Value)));
        }

        /// <summary>Returns the size as text.</summary>
        public override string ToString() => Value.ToString();
    }
}
