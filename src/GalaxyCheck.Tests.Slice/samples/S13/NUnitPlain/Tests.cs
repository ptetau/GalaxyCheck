namespace S13Sample;

// NUnit loads an assembly only if it refers to NUnit, so each test class has this attribute.
[NUnit.Framework.TestFixture]
public class PlainTests
{
    [GalaxyCheck.Property]
    public bool PlainDisabled(int x)
    {
        Probe.Log("PlainDisabled", x);
        return x < 10;
    }

    [GalaxyCheck.Example(1, 2)]
    [GalaxyCheck.Example(3, 4)]
    [GalaxyCheck.Property(Iterations = 10)]
    public bool ExOrder(int first, int second)
    {
        Probe.Log("ExOrder", first + "," + second);
        return true;
    }

    [GalaxyCheck.Example(3, 4)]
    [GalaxyCheck.Property]
    public bool ExFails(int first, int second)
    {
        Probe.Log("ExFails", first + "," + second);
        return first != 3;
    }

    [GalaxyCheck.Example(1)]
    [GalaxyCheck.Property]
    public bool ExMissing(int first, int second)
    {
        Probe.Log("ExMissing", first + "," + second);
        return true;
    }

    [GalaxyCheck.Example("a")]
    [GalaxyCheck.Property]
    public bool ExWrongType(int number)
    {
        Probe.Log("ExWrongType", number);
        return true;
    }
}
