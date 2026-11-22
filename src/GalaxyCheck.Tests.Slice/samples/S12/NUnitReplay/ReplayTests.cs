using GalaxyCheck;

namespace Sample;

[NUnit.Framework.TestFixture]
public class ReplayTests
{
    // The text @@REPLAY@@ is replaced by the test of the repository with a replay string.
    [Property]
    [Replay("@@REPLAY@@")]
    public bool Replayed(int x)
    {
        Probe.Append("replay", x.ToString());
        return x < 10;
    }
}
