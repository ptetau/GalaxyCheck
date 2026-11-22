using GalaxyCheck.Internal;
using System;

namespace GalaxyCheck
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public partial class SampleAttribute : PropertyAttribute
    {
        internal override IPropertyRunner Runner => new PropertySampleRunner();
    }
}
