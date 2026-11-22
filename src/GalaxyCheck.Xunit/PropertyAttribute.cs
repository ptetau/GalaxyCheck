using Xunit;
using Xunit.Sdk;

namespace GalaxyCheck
{
    [XunitTestCaseDiscoverer("GalaxyCheck.Internal.Infrastructure.Xunit.PropertyDiscoverer", "GalaxyCheck.Xunit")]
    public partial class PropertyAttribute : FactAttribute
    {
        internal string? SkipReason => Skip?.Length > 0 ? Skip : null;
    }
}
