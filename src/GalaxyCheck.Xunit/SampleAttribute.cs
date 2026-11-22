using Xunit.Sdk;

namespace GalaxyCheck
{
    [XunitTestCaseDiscoverer("GalaxyCheck.Internal.Infrastructure.Xunit.PropertyDiscoverer", "GalaxyCheck.Xunit")]
    public partial class SampleAttribute
    {
    }
}
