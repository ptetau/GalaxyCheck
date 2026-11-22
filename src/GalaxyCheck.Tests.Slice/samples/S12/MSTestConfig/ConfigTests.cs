using GalaxyCheck;
using GalaxyCheck.Configuration;

namespace Sample;

public class GlobalConfig : IConfigureGlobal
{
    public void Configure(IGlobalConfiguration instance)
    {
        instance.Properties.DefaultIterations = 7;
    }
}

[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
public class ConfigTests
{
    [Property]
    public void DefaultIterations(int x)
    {
        Probe.Append("config", x.ToString());
    }
}
