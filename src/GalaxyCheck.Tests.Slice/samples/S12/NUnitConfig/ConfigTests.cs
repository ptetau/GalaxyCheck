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

[NUnit.Framework.TestFixture]
public class ConfigTests
{
    [Property]
    public void DefaultIterations(int x)
    {
        Probe.Append("config", x.ToString());
    }
}
