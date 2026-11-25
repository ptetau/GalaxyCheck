using System;

namespace GalaxyCheck
{
    /// <summary>The base of an attribute that gives the generator of a test parameter.</summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    [global::Verifiable.Contracts.Unverifiable]
    public abstract class GenAttribute : Attribute
    {
        /// <summary>The generator of the parameter.</summary>
        public abstract IGen Value { get; }
    }
}
