namespace S13Sample;

// NUnit loads an assembly only if it refers to NUnit, so each test class has this attribute.
[NUnit.Framework.TestFixture]
public class MemoryTests
{
    [GalaxyCheck.Property]
    public bool MemSwitchable(int x)
    {
        Probe.Log("MemSwitchable", x);
        return x < 10 || Probe.Hold;
    }

    [GalaxyCheck.Property]
    public bool MemOther(int x)
    {
        Probe.Log("MemOther", x);
        return x < 50;
    }

    [GalaxyCheck.Replay(FixedReplay.Value)]
    [GalaxyCheck.Property]
    public bool MemReplayOverride(int x)
    {
        Probe.Log("MemReplayOverride", x);
        return x < 50;
    }
}
