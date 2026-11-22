using GalaxyCheck;
using GalaxyCheck.Gens;

namespace S11.Snapshot;

public class SnapshotTests
{
    // The environment variable GC_VARIANT picks the values: B changes them.
    [Test, GalaxyCheck.GenSnapshot(Iterations = 5)]
    public IGen<int> Values() =>
        Environment.GetEnvironmentVariable("GC_VARIANT") == "B"
            ? Gen.Int32().Between(2000, 3000)
            : Gen.Int32().Between(0, 1000);
}
