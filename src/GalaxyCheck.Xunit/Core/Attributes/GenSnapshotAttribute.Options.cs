using System;

namespace GalaxyCheck
{
    // The options of [GenSnapshot] in each framework package. Each package declares the base class, the
    // constructor that records the file of the test, and TestFileName.
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed partial class GenSnapshotAttribute
    {
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
            set
            {
                NullableIterations = value;
            }
        }

        internal int? NullableSize { get; private set; }

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
            set
            {
                NullableSize = value;
            }
        }

        internal int? NullableIterations { get; private set; }
    }
}
