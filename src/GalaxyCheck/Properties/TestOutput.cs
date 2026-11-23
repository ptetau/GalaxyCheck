using System;

namespace GalaxyCheck
{
    public partial class Property
    {
        /// <summary>The outcome of one test case.</summary>
#pragma warning disable IDE1006 // Naming Styles
        public interface TestOutput
#pragma warning restore IDE1006 // Naming Styles
        {
            /// <summary>Whether the test case passed or failed.</summary>
            TestResult Result { get; }

            /// <summary>The exception of a failed test case, or null.</summary>
            Exception? Exception { get; }
        }
    }
}
