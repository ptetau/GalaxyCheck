using GalaxyCheck;
using GalaxyCheck.Gens;

namespace S11.Replay;

public class ReplayTests
{
    // The test replaces __REPLAY__ with the replay string of the counterexample of this property.
    [GalaxyCheck.Replay("__REPLAY__")]
    [GalaxyCheck.Property]
    public Property<int> ReplayedBelow10() =>
        Property.ForAll(Gen.Int32(), x =>
        {
            Log.Write("ReplayedBelow10", x.ToString());
            return x < 10;
        });
}
