using FluentAssertions;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S6_ReplayTests
{
    [Fact]
    public void test_001_S4_R21_replay_string_reproduces_the_counterexample()
    {
        var problems = new List<string>();
        Replays("Double", Gen.Double(), x => x < 100.0, problems);
        Replays("UInt64", Gen.UInt64(), x => x < 1000UL, problems);
        Replays("Dictionary", Gen.Dictionary(Gen.Int32(), Gen.Boolean()), d => d.Count < 3, problems);

        problems.Should().BeEmpty("each replay must give the same counterexample in one test case: {0}", string.Join("; ", problems));
    }

    private static void Replays<T>(string name, IGen<T> gen, Func<T, bool> predicate, List<string> problems)
    {
        try
        {
            var property = Property.ForAll(gen, predicate);
            var first = property.Check(iterations: 100, seed: 1);
            if (!first.Falsified || first.Counterexample is null)
            {
                problems.Add($"{name}: the property did not fail");
                return;
            }

            var second = property.Check(replay: first.Counterexample.Replay);
            if (!second.Falsified || second.Counterexample is null)
            {
                problems.Add($"{name}: the replay did not fail");
                return;
            }

            var firstText = S6Support.Show(first.Counterexample.Value);
            var secondText = S6Support.Show(second.Counterexample.Value);
            if (firstText != secondText)
            {
                problems.Add($"{name}: the counterexample changed from {firstText} to {secondText}");
            }

            if (second.Iterations != 1)
            {
                problems.Add($"{name}: the replay ran {second.Iterations} test cases");
            }
        }
        catch (Exception e)
        {
            problems.Add($"{name}: {e.GetType().Name}: {e.Message}");
        }
    }
}
