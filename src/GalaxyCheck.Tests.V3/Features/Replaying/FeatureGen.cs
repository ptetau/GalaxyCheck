using GalaxyCheck.Stable;

namespace GalaxyCheck.Tests.Features.Replaying;

internal static class FeatureGen
{
    public static Stable.IGen<string> NotBase64() => Stable.Gen.Constant("0");

    public static Stable.IGen<GalaxyCheck.Runners.Replaying.Replay> Replay(bool allowEmptyPath = true) =>
        from seed in DomainGen.Seed()
        from size in DomainGen.Size()
        from path in ShrinkPath(allowEmptyPath)
        select new GalaxyCheck.Runners.Replaying.Replay(seed, size, path.Select(choice => (ulong)(uint)choice).ToArray());

    public static Stable.IGen<IReadOnlyList<int>> ShrinkPath(bool allowEmpty = true) =>
        allowEmpty ? Stable.Gen.Int32().ListOf() : Stable.Gen.Int32().ListOf().WithCountGreaterThanEqual(1);
}
