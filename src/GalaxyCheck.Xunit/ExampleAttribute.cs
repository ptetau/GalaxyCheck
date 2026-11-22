using System;

namespace GalaxyCheck
{
    /// <summary>Fixed inputs that run before the generated test cases.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ExampleAttribute : Attribute
    {
        public object?[] Inputs { get; }

        public ExampleAttribute(params object?[] inputs)
        {
            Inputs = inputs;
        }
    }

    /// <summary>Suppresses the named health checks of a property.</summary>
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
