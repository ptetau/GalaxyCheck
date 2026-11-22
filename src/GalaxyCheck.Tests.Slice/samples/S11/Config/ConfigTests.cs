using GalaxyCheck.Configuration;

namespace S11.Config;

public class GlobalSettings : IConfigureGlobal
{
    public void Configure(IGlobalConfiguration instance)
    {
        instance.Properties.DefaultIterations = 7;
    }
}

public class ConfigTests
{
    [Test, GalaxyCheck.PropertyTest]
    public void CountedRun(int x) => Log.Write("CountedRun", x.ToString());
}
