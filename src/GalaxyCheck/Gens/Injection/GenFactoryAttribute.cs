using GalaxyCheck.Gens;
using System;

namespace GalaxyCheck
{
    /// <summary>The base of an attribute that gives the generator factory of a test method or class.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public abstract class GenFactoryAttribute : Attribute
    {
        /// <summary>The factory that builds the generators of the parameters.</summary>
        public abstract IGenFactory Value { get; }
    }
}
