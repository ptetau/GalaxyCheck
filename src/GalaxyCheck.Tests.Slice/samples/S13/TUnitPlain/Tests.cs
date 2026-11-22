namespace S13Sample;

public class PlainTests
{
    [Test, GalaxyCheck.PropertyTest]
    public bool PlainDisabled(int x)
    {
        Probe.Log("PlainDisabled", x);
        return x < 10;
    }

    [GalaxyCheck.Example(1, 2)]
    [GalaxyCheck.Example(3, 4)]
    [Test, GalaxyCheck.PropertyTest(Iterations = 10)]
    public bool ExOrder(int first, int second)
    {
        Probe.Log("ExOrder", first + "," + second);
        return true;
    }

    [GalaxyCheck.Example(3, 4)]
    [Test, GalaxyCheck.PropertyTest]
    public bool ExFails(int first, int second)
    {
        Probe.Log("ExFails", first + "," + second);
        return first != 3;
    }

    [GalaxyCheck.Example(1)]
    [Test, GalaxyCheck.PropertyTest]
    public bool ExMissing(int first, int second)
    {
        Probe.Log("ExMissing", first + "," + second);
        return true;
    }

    [GalaxyCheck.Example("a")]
    [Test, GalaxyCheck.PropertyTest]
    public bool ExWrongType(int number)
    {
        Probe.Log("ExWrongType", number);
        return true;
    }
}
