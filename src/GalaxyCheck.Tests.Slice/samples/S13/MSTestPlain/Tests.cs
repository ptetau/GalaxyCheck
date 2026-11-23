namespace S13Sample;

[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
public class PlainTests
{
    [GalaxyCheck.Property]
    public void PlainDisabled(int x)
    {
        Probe.Log("PlainDisabled", x);
        if (!(x < 10)) throw new Exception("the int is not below 10");
    }

    [GalaxyCheck.Example(1, 2)]
    [GalaxyCheck.Example(3, 4)]
    [GalaxyCheck.Property(Iterations = 10)]
    public void ExOrder(int first, int second)
    {
        Probe.Log("ExOrder", first + "," + second);
    }

    [GalaxyCheck.Example(3, 4)]
    [GalaxyCheck.Property]
    public void ExFails(int first, int second)
    {
        Probe.Log("ExFails", first + "," + second);
        if (first == 3) throw new Exception("the first parameter is 3");
    }

    [GalaxyCheck.Example(1)]
    [GalaxyCheck.Property]
    public void ExMissing(int first, int second)
    {
        Probe.Log("ExMissing", first + "," + second);
    }

    [GalaxyCheck.Example("a")]
    [GalaxyCheck.Property]
    public void ExWrongType(int number)
    {
        Probe.Log("ExWrongType", number);
    }
}
