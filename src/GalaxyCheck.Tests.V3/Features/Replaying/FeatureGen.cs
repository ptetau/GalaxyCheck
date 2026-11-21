
namespace GalaxyCheck.Tests.Features.Replaying;

internal static class FeatureGen
{
    public static IGen<string> NotBase64() => Gen.Constant("0");

    public static IGen<GalaxyCheck.Runners.Replaying.Replay> Replay(bool allowEmptyPath = true) =>
        from seed in DomainGen.Seed()
        from size in DomainGen.Size()
        from path in ShrinkPath(allowEmptyPath)
        select new GalaxyCheck.Runners.Replaying.Replay(seed, size, path.Select(choice => (ulong)(uint)choice).ToArray());

    public static IGen<IReadOnlyList<int>> ShrinkPath(bool allowEmpty = true) =>
        allowEmpty ? Gen.Int32().ListOf() : Gen.Int32().ListOf().WithCountGreaterThanEqual(1);
}
