using GalaxyCheck.Internal;
using System;

namespace GalaxyCheck
{
    /// <summary>Marks a method whose test fails with a list of sampled inputs, to look at a generator.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public partial class SampleAttribute : PropertyAttribute
    {
        internal override IPropertyRunner Runner => new PropertySampleRunner();
    }
}
