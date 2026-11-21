using GalaxyCheck.Runners.Replaying;

namespace GalaxyCheck.Tests.Features.Replaying;

public class AboutReplayEncoding
{
    [Stable.Property]
    public Stable.Property EncodeDecodeRoundtrip()
    {
        return Stable.Property.ForAll(DomainGen.Seed(), DomainGen.Size(), FeatureGen.ShrinkPath(), Test);

        static void Test(int seed, int size, IReadOnlyList<int> choices)
        {
            var replay0 = new Replay(seed, size, choices.Select(choice => (ulong)(uint)choice).ToArray());
            var replay1 = ReplayEncoding.Decode(ReplayEncoding.Encode(replay0.Seed, replay0.Size, replay0.Choices, replay0.Choices.Length));

            replay1.Should().BeEquivalentTo(replay0);
        }
    }
}
