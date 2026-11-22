using GalaxyCheck;

namespace Sample;

[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
public class ReplayTests
{
    // The text @@REPLAY@@ is replaced by the test of the repository with a replay string.
    [Property]
    [Replay("@@REPLAY@@")]
    public void Replayed(int x)
    {
        Probe.Append("replay", x.ToString());
        if (!(x < 10)) throw new Exception("the int is not below 10");
    }
}
