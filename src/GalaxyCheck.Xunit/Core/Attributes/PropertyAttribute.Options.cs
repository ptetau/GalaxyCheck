using GalaxyCheck.Internal;
using System;

namespace GalaxyCheck
{
    // The options of [Property] in each framework package. Each package declares the base class and the discovery.
    /// <summary>Marks a method as a property test: GalaxyCheck generates its parameters and runs it for each test case.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public partial class PropertyAttribute
    {
        /// <summary>The time limit of one test case, in milliseconds.</summary>
        public int TimeoutMilliseconds
        {
            get => NullableTimeoutMilliseconds ?? 0;
            set => NullableTimeoutMilliseconds = value;
        }

        internal int? NullableTimeoutMilliseconds { get; private set; }

        /// <summary>False switches off the exhaustive run of a small input space.</summary>
        public bool Exhaustive
        {
            get => NullableExhaustive ?? true;
            set => NullableExhaustive = value;
        }

        internal bool? NullableExhaustive { get; private set; }

        /// <summary>The count of test cases. The default comes from the global configuration, or 100.</summary>
        public int Iterations
        {
            get
            {
                if (NullableIterations == null)
                {
                    throw new InvalidOperationException($"{nameof(Iterations)} was not set, and does not have a default value");
                }

                throw new InvalidOperationException("");
            }
            set { NullableIterations = value; }
        }

        internal int? NullableIterations { get; private set; }

        /// <summary>The most shrinks of a counterexample. The default comes from the global configuration, or 500.</summary>
        public int ShrinkLimit
        {
            get
            {
                if (NullableShrinkLimit == null)
                {
                    throw new InvalidOperationException($"{nameof(ShrinkLimit)} was not set, and does not have a default value");
                }

                throw new InvalidOperationException("");
            }
            set { NullableShrinkLimit = value; }
        }

        internal int? NullableShrinkLimit { get; private set; }

        /// <summary>The seed of the run. With no seed, GALAXYCHECK_SEED or GALAXYCHECK_AGENT can set it, else it is random.</summary>
        public int Seed
        {
            get
            {
                if (NullableSeed == null)
                {
                    throw new InvalidOperationException($"{nameof(Seed)} was not set, and does not have a default value");
                }

                throw new InvalidOperationException("");
            }
            set { NullableSeed = value; }
        }

        internal int? NullableSeed { get; private set; }

        /// <summary>A fixed size for each test case, from 0 to 100.</summary>
        public int Size
        {
            get
            {
                if (NullableSize == null)
                {
                    throw new InvalidOperationException($"{nameof(Size)} was not set, and does not have a default value");
                }

                throw new InvalidOperationException("");
            }
            set { NullableSize = value; }
        }

        internal int? NullableSize { get; private set; }

        internal virtual IPropertyRunner Runner => new PropertyAssertRunner();
    }
}
