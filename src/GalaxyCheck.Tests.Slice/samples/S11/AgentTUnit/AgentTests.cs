namespace AgentSample;

public class AgentTests
{
    [Test, GalaxyCheck.PropertyTest(Seed = 1, Iterations = 1000)]
    public bool IntBelow10(int x) => x < 10;

    [Test, GalaxyCheck.PropertyTest(Seed = 1, Iterations = 1000)]
    public bool OtherBelow5(int x) => x < 5;
}
