using System;
using System.Threading.Tasks;

// Stubs. Each attribute is a plain attribute until its slice is built, so the test framework finds no test.

namespace GalaxyCheck
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PropertyAttribute : Attribute
    {
        public int Iterations { get; set; }

        public int ShrinkLimit { get; set; }

        public int Seed { get; set; }

        public int Size { get; set; }

        public int TimeoutMilliseconds { get; set; }

        public bool Exhaustive { get; set; } = true;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SampleAttribute : PropertyAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class GenSnapshotAttribute : Attribute
    {
        public int Iterations { get; set; }

        public int Size { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ReplayAttribute : Attribute
    {
        public string Replay { get; }

        public ReplayAttribute(string replay)
        {
            Replay = replay;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class MemberGenAttribute : Attribute
    {
        public string MemberName { get; }

        public MemberGenAttribute(string memberName)
        {
            MemberName = memberName;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ExampleAttribute : Attribute
    {
        public object?[] Inputs { get; }

        public ExampleAttribute(params object?[] inputs)
        {
            Inputs = inputs;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SuppressHealthCheckAttribute : Attribute
    {
        public HealthCheck[] Checks { get; }

        public SuppressHealthCheckAttribute(params HealthCheck[] checks)
        {
            Checks = checks;
        }
    }
}

namespace GalaxyCheck.Configuration
{
    public interface IConfigureGlobal
    {
        void Configure(IGlobalConfiguration instance);
    }

    public interface IGlobalConfiguration
    {
        IGlobalPropertyConfiguration Properties { get; }

        IGlobalGenSnapshotConfiguration GenSnapshots { get; }

        IGlobalFailureMemoryConfiguration FailureMemory { get; }
    }

    public interface IGlobalPropertyConfiguration
    {
        int DefaultIterations { get; set; }

        int DefaultShrinkLimit { get; set; }

        int? Seed { get; set; }
    }

    public interface IGlobalGenSnapshotConfiguration
    {
        int DefaultIterations { get; set; }

        int DefaultSize { get; set; }

        Func<ISnapshot, Task>? AssertSnapshotMatches { get; set; }
    }

    public interface IGlobalFailureMemoryConfiguration
    {
        bool Enabled { get; set; }
    }

    public interface ISnapshot
    {
        object?[] Input { get; }

        object? Output { get; }

        string TestFileName { get; }

        string TestClassName { get; }

        string TestMethodName { get; }

        int Seed { get; }
    }
}
