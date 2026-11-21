using GalaxyCheck;

namespace S10Sample;

// The xunit v2 version of the tests in ../Main/Props.cs. The text of a failure must be the same in both.
public class Props
{
    [Property(Seed = 42)]
    public bool TheIntIsBelow10(int x) => x < 10;

    [Property]
    [Replay("0")]
    public bool ReplayZero(int x) => true;
}
