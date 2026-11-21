namespace AgentSample;

public class QuietTests
{
    [GalaxyCheck.Property(Iterations = 20)]
    public bool Passes(int x) => true;

    [GalaxyCheck.Property(Iterations = 1000)]
    public bool IntBelow10(int x) => x < 10;

    [GalaxyCheck.Replay("0")]
    [GalaxyCheck.Property]
    public bool BadReplay(int x) => true;

    [GalaxyCheck.Property]
    public bool BadMemberGen([GalaxyCheck.MemberGen("NoSuchMember")] int x) => true;

    [GalaxyCheck.Example(1)]
    [GalaxyCheck.Property]
    public bool BadExample(int first, int second) => true;
}
