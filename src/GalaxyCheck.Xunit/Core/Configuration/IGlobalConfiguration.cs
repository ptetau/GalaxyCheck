using System;
using System.Threading.Tasks;

namespace GalaxyCheck.Configuration
{
    /// <summary>The global configuration of a test assembly.</summary>
    public interface IGlobalConfiguration
    {
        /// <summary>The defaults of each property test.</summary>
        IGlobalPropertyConfiguration Properties { get; }

        /// <summary>The defaults and the store of the [GenSnapshot] tests.</summary>
        IGlobalGenSnapshotConfiguration GenSnapshots { get; }

        /// <summary>The settings of the failure memory.</summary>
        IGlobalFailureMemoryConfiguration FailureMemory { get; }
    }

    /// <summary>The settings of the failure memory, the file .galaxycheck/failures.json.</summary>
    public interface IGlobalFailureMemoryConfiguration
    {
        /// <summary>True (the default) to save each failure and to try it first in the next run.</summary>
        bool Enabled { get; set; }
    }

    /// <summary>The defaults of each property test.</summary>
    public interface IGlobalPropertyConfiguration
    {
        /// <summary>The count of test cases of a property with no Iterations option.</summary>
        int DefaultIterations { get; set; }

        /// <summary>The most shrinks of a property with no ShrinkLimit option.</summary>
        int DefaultShrinkLimit { get; set; }

        /// <summary>The seed of each property with no Seed option, or null.</summary>
        int? Seed { get; set; }
    }

    /// <summary>The defaults and the store of the [GenSnapshot] tests.</summary>
    public interface IGlobalGenSnapshotConfiguration
    {
        /// <summary>The count of snapshots of a test with no Iterations option.</summary>
        int DefaultIterations { get; set; }

        /// <summary>The size of a test with no Size option.</summary>
        int DefaultSize { get; set; }

        /// <summary>Compares a snapshot with the stored one, and throws when they differ.</summary>
        Func<ISnapshot, Task>? AssertSnapshotMatches { get; set; }
    }

    /// <summary>One snapshot of a [GenSnapshot] test.</summary>
    public interface ISnapshot
    {
        /// <summary>The sampled input.</summary>
        object?[] Input { get; }

        /// <summary>The value that the test method returned.</summary>
        object? Output { get; }

        /// <summary>The path of the file of the test method.</summary>
        string TestFileName { get; }

        /// <summary>The name of the test class.</summary>
        string TestClassName { get; }

        /// <summary>The name of the test method.</summary>
        string TestMethodName { get; }

        /// <summary>The seed of the snapshot.</summary>
        int Seed { get; }
    }
}
