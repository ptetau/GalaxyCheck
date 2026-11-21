using GalaxyCheck;

namespace Sample;

[NUnit.Framework.TestFixture]
public class ReplayTests
{
    // The text H4sIAAAAAAAAAzM0szQwMTc2tzDWMzHWM7QCQSMA5eTboRUAAAA= is replaced by the test of the repository with a replay string.
    [Property]
    [Replay("H4sIAAAAAAAAAzM0szQwMTc2tzDWMzHWM7QCQSMA5eTboRUAAAA=")]
    public bool Replayed(int x)
    {
        Probe.Append("replay", x.ToString());
        return x < 10;
    }
}
