namespace S13Sample;

[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
public class MemoryTests
{
    [GalaxyCheck.Property]
    public void MemSwitchable(int x)
    {
        Probe.Log("MemSwitchable", x);
        if (!(x < 10 || Probe.Hold)) throw new Exception("the int is not below 10");
    }

    [GalaxyCheck.Property]
    public void MemOther(int x)
    {
        Probe.Log("MemOther", x);
        if (!(x < 50)) throw new Exception("the int is not below 50");
    }

    [GalaxyCheck.Replay(FixedReplay.Value)]
    [GalaxyCheck.Property]
    public void MemReplayOverride(int x)
    {
        Probe.Log("MemReplayOverride", x);
        if (!(x < 50)) throw new Exception("the int is not below 50");
    }
}
