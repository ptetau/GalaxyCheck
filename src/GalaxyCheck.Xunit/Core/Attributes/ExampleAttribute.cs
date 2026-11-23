using System;

namespace GalaxyCheck
{
    /// <summary>Fixed inputs that run before the generated test cases.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ExampleAttribute : Attribute
    {
        /// <summary>The inputs of the example, in the order of the parameters.</summary>
        public object?[] Inputs { get; }

        /// <summary>Creates an example with one input for each parameter, in order.</summary>
        public ExampleAttribute(params object?[] inputs)
        {
            Inputs = inputs;
        }
    }

    /// <summary>Suppresses the named health checks of a property, or of each property of a class.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class SuppressHealthCheckAttribute : Attribute
    {
        /// <summary>The health checks that the method or class turns off.</summary>
        public HealthCheck[] Checks { get; }

        /// <summary>Turns off the given health checks.</summary>
        public SuppressHealthCheckAttribute(params HealthCheck[] checks)
        {
            Checks = checks;
        }
    }
}
