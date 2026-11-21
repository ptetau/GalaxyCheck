namespace AgentSample;

[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
public class AgentTests
{
    [GalaxyCheck.Property(Seed = 1, Iterations = 1000)]
    public bool IntBelow10(int x) => x < 10;

    [GalaxyCheck.Property(Seed = 1, Iterations = 1000)]
    public bool OtherBelow5(int x) => x < 5;
}
