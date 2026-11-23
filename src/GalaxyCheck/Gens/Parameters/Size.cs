using System;

namespace GalaxyCheck.Gens.Parameters
{
    /// <summary>The size of a test case, from 0 to 100. A larger size gives larger values.</summary>
    public record Size
    {
        /// <summary>The smallest size, 0.</summary>
        public static Size Zero { get; } = new Size(0);
        /// <summary>The largest size, 100.</summary>
        public static Size Max { get; } = new Size(100);

        /// <summary>Creates a size from 0 to 100.</summary>
        public Size(int value)
        {
            if (value < 0 || value > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "'value' must be between 0 and 100");
            }

            Value = value;
        }

        /// <summary>The size, from 0 to 100.</summary>
        public int Value { get; init; }

        /// <summary>Returns the next size, which wraps from 99 to 0.</summary>
        public Size Increment() => new Size((Value + 1) % 100);

        /// <summary>Returns the size five steps up, which wraps at 100.</summary>
        public Size BigIncrement() => new Size((Value + 5) % 100);

        /// <summary>Returns the size as text.</summary>
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
