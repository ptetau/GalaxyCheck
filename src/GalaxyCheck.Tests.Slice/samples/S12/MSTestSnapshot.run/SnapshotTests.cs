using GalaxyCheck;

namespace Sample;

[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
public class SnapshotTests
{
    // The text Gen.Int32().Between(0, 100) is replaced by the test of the repository with a generator.
    [GenSnapshot(Iterations = 10)]
    public IGen<int> Snapshot() => Gen.Int32().Between(0, 100);
}
