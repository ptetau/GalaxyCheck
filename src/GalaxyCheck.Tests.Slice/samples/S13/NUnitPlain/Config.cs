using GalaxyCheck.Configuration;

namespace S13Sample;

public sealed class SwitchOffFailureMemory : IConfigureGlobal
{
    public void Configure(IGlobalConfiguration instance)
    {
        instance.FailureMemory.Enabled = false;
    }
}
