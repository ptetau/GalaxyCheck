namespace AgentSample;

[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
public class AgentTests
{
    [GalaxyCheck.Property(Seed = 1, Iterations = 1000)]
    public void IntBelow10(int x)
    {
        if (!(x < 10)) throw new InvalidOperationException("the int is not below 10");
    }

    [GalaxyCheck.Property(Seed = 1, Iterations = 1000)]
    public void OtherBelow5(int x)
    {
        if (!(x < 5)) throw new InvalidOperationException("the int is not below 5");
    }
}
