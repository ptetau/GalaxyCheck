namespace S13Sample;

public class MemoryTests
{
    [Test, GalaxyCheck.PropertyTest]
    public bool MemSwitchable(int x)
    {
        Probe.Log("MemSwitchable", x);
        return x < 10 || Probe.Hold;
    }

    [Test, GalaxyCheck.PropertyTest]
    public bool MemOther(int x)
    {
        Probe.Log("MemOther", x);
        return x < 50;
    }

    [GalaxyCheck.Replay(FixedReplay.Value)]
    [Test, GalaxyCheck.PropertyTest]
    public bool MemReplayOverride(int x)
    {
        Probe.Log("MemReplayOverride", x);
        return x < 50;
    }
}
