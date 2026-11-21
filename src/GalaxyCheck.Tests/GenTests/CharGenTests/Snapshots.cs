using GalaxyCheck;
using Snapshooter;
using Snapshooter.Xunit;
using System.Linq;
using Xunit;

namespace Tests.V2.GenTests.CharGenTests
{
    public class Snapshots
    {
        [Fact]
        public void Values()
        {
            var seeds = Enumerable.Range(0, 3);

            foreach (var seed in seeds)
            {
                var sample = Gen.Char().Select(c => char.IsControl(c) ? "<control>" : c.ToString()).Sample(seed: seed);

                var nameExtension = string.Join("_", new[]
                {
                    $"Seed_{seed}"
                });

                Snapshot.Match(sample, SnapshotNameExtension.Create(nameExtension));
            }
        }

        [Fact]
        public void ExampleSpaces()
        {
            var seeds = Enumerable.Range(0, 3);

            foreach (var seed in seeds)
            {
                var sample = Gen.Char()
                    .Select(c => char.IsControl(c) ? "<control>" : c.ToString())
                    .RenderOneTraversal(seed: seed, size: 75, x => x);

                var nameExtension = string.Join("_", new[]
                {
                    $"Seed_{seed}"
                });

                Snapshot.Match(sample, SnapshotNameExtension.Create(nameExtension));
            }
        }
    }
}
