using System;

namespace GalaxyCheck
{
    /// <summary>The entry point of the generators, for example <c>Gen.Int32()</c> or <c>Gen.Create&lt;T&gt;()</c>.</summary>
    public static partial class Gen
    {
        /// <summary>
        /// A flag which indicates how values produced by generators should scale with respect to the size parameter.
        /// </summary>
        public enum Bias
        {
            /// <summary>
            /// Generated values should not be biased by the size parameter.
            /// </summary>
            None = 0,

            /// <summary>
            /// Generated values should scale exponentially with the size parameter.
            /// </summary>
            WithSize = 2
        }

        /// <summary>The kinds of characters that a char or string generator uses. Combine them with <c>|</c>.</summary>
        [Flags]
        public enum CharType
        {
            /// <summary>Space, tab, and line break characters.</summary>
            Whitespace = 1 << 0,

            /// <summary>The letters a to z and A to Z.</summary>
            Alphabetical = 1 << 1,

            /// <summary>The digits 0 to 9.</summary>
            Numeric = 1 << 2,

            /// <summary>Printable ASCII symbols, for example <c>!</c> and <c>@</c>.</summary>
            Symbol = 1 << 3,

            /// <summary>Characters above the ASCII range.</summary>
            Extended = 1 << 4,

            /// <summary>Control characters.</summary>
            Control = 1 << 5,

            /// <summary>Each kind of character.</summary>
            All = Whitespace | Alphabetical | Numeric | Symbol | Extended | Control
        }

        /// <summary>
        /// A container which contains generators for advanced usage.
        /// </summary>
        public static partial class Advanced
        {
        }
    }
}
