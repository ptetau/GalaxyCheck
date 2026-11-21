using FluentAssertions;
using GalaxyCheck.Runners;
using GalaxyCheck.Stateful;
using Xunit;

namespace GalaxyCheck.Tests.Slice;

public class S9_StatefulTests
{
    private sealed class Counter
    {
        public int Value;
        public int RunCount;
        public int ValueAtFirstRun = -1;
        public bool IncrementAddsTwoAtThree;
        public bool DecrementThrows;
        public bool DecrementRanAtZero;
    }

    private sealed class Counters
    {
        public readonly List<Counter> Systems = new();
        public int NewModelCalls;
        public bool IncrementAddsTwoAtThree;
        public bool DecrementThrows;

        public Counter NewSystem()
        {
            var counter = new Counter
            {
                IncrementAddsTwoAtThree = IncrementAddsTwoAtThree,
                DecrementThrows = DecrementThrows,
            };
            Systems.Add(counter);
            return counter;
        }

        public int NewModel()
        {
            NewModelCalls++;
            return 0;
        }

        public bool AnyDecrementRanAtZero => Systems.Any(s => s.DecrementRanAtZero);
    }

    private static void Enter(Counter system)
    {
        if (system.RunCount == 0)
        {
            system.ValueAtFirstRun = system.Value;
        }

        system.RunCount++;
    }

    private sealed class Increment : Command<Counter, int>
    {
        public override void Run(Counter system)
        {
            Enter(system);
            system.Value += system.IncrementAddsTwoAtThree && system.Value == 3 ? 2 : 1;
        }

        public override int NextModel(int model) => model + 1;

        public override bool Postcondition(Counter system, int modelBefore, int modelAfter) => system.Value == modelAfter;

        public override string ToString() => "Increment";
    }

    private sealed class Decrement : Command<Counter, int>
    {
        public override bool Precondition(int model) => model > 0;

        public override void Run(Counter system)
        {
            Enter(system);
            if (system.Value == 0)
            {
                system.DecrementRanAtZero = true;
            }

            if (system.DecrementThrows)
            {
                throw new InvalidOperationException("decrement failed");
            }

            system.Value -= 1;
        }

        public override int NextModel(int model) => model - 1;

        public override bool Postcondition(Counter system, int modelBefore, int modelAfter) => system.Value == modelAfter;

        public override string ToString() => "Decrement";
    }

    private static IGen<Command<Counter, int>> NextCommand(int model) =>
        model > 0
            ? Gen.Element(new Command<Counter, int>[] { new Increment(), new Decrement() })
            : Gen.Element(new Command<Counter, int>[] { new Increment() });

    private static Property<IReadOnlyList<Command<Counter, int>>> CreateProperty(Counters counters) =>
        StatefulProperty.Create<Counter, int>(counters.NewSystem, counters.NewModel, NextCommand);

    private static string FailureMessage(Property<IReadOnlyList<Command<Counter, int>>> property)
    {
        var act = () => property.Assert(iterations: 300, seed: 1);
        return act.Should().Throw<PropertyFailedException>().Which.Message;
    }

    [Fact]
    public void test_001_S7_R42_no_decrement_at_model_zero_in_1000_sequences()
    {
        var counters = new Counters();
        var gen = StatefulGen.Commands<Counter, int>(counters.NewModel, NextCommand);

        var sequences = gen.Sample(iterations: 1000, seed: 7);

        sequences.Should().HaveCount(1000);
        sequences.Any(s => s.Any(c => c is Decrement)).Should().BeTrue("the sequences must hold decrements for the check to mean anything");
        foreach (var sequence in sequences)
        {
            var model = 0;
            foreach (var command in sequence)
            {
                if (command is Decrement)
                {
                    model.Should().BeGreaterThan(0, "a decrement must never be at a position where the model is 0");
                }

                command.Precondition(model).Should().BeTrue();
                model = command.NextModel(model);
            }
        }
    }

    [Fact]
    public void test_001_S7_R43_correct_counter_passes_100_sequences_each_with_new_system_and_model()
    {
        var counters = new Counters();
        var property = CreateProperty(counters);

        var result = property.Check(iterations: 100, seed: 3);

        result.Falsified.Should().BeFalse();
        result.Iterations.Should().Be(100);
        counters.Systems.Should().HaveCount(100, "each sequence starts with a new system");
        counters.Systems.Distinct().Should().HaveCount(100);
        counters.Systems.Should().OnlyContain(s => s.ValueAtFirstRun == 0 || s.ValueAtFirstRun == -1, "each system starts at its initial value");
        counters.NewModelCalls.Should().BeGreaterThanOrEqualTo(100, "each sequence starts with a new model");
    }

    [Fact]
    public void test_001_S7_R43_increment_that_adds_two_at_three_fails_by_a_postcondition()
    {
        var counters = new Counters { IncrementAddsTwoAtThree = true };
        var property = CreateProperty(counters);

        var result = property.Check(iterations: 300, seed: 1);

        result.Falsified.Should().BeTrue();
        result.Counterexample.Should().NotBeNull();
        var message = FailureMessage(CreateProperty(new Counters { IncrementAddsTwoAtThree = true }));
        message.Should().Contain("<- failed");
    }

    [Fact]
    public void test_001_S7_R43_decrement_that_throws_an_unexpected_exception_fails()
    {
        var counters = new Counters { DecrementThrows = true };
        var property = CreateProperty(counters);

        var result = property.Check(iterations: 300, seed: 1);

        result.Falsified.Should().BeTrue();
        result.Counterexample.Should().NotBeNull();
        result.Counterexample!.Value.Should().Contain(c => c is Decrement);
    }

    [Fact]
    public void test_001_S7_R44_failure_shrinks_to_four_increments()
    {
        var counters = new Counters { IncrementAddsTwoAtThree = true };
        var property = CreateProperty(counters);

        var result = property.Check(iterations: 300, seed: 1);

        result.Falsified.Should().BeTrue();
        var shrunk = result.Counterexample!.Value;
        shrunk.Should().HaveCount(4);
        shrunk.Should().AllBeOfType<Increment>();
    }

    [Fact]
    public void test_001_S7_R44_each_precondition_holds_in_each_shrunk_sequence_that_runs()
    {
        var counters = new Counters { IncrementAddsTwoAtThree = true };
        var property = CreateProperty(counters);

        var result = property.Check(iterations: 300, seed: 5);

        result.Falsified.Should().BeTrue();
        counters.AnyDecrementRanAtZero.Should().BeFalse("no sequence that ran, shrunk or not, may run a decrement at model 0");
        var model = 0;
        foreach (var command in result.Counterexample!.Value)
        {
            command.Precondition(model).Should().BeTrue();
            model = command.NextModel(model);
        }
    }

    [Fact]
    public void test_001_S7_R45_report_lists_four_commands_in_order_and_names_the_fourth_as_failed()
    {
        var message = FailureMessage(CreateProperty(new Counters { IncrementAddsTwoAtThree = true }));

        var lines = message.Split('\n').Select(l => l.Trim()).ToList();
        lines.Should().Contain("1. Increment", message);
        lines.Should().Contain("2. Increment", message);
        lines.Should().Contain("3. Increment", message);
        lines.Should().Contain("4. Increment <- failed", message);
        lines.Should().NotContain(l => l.StartsWith("5. "), message);
        lines.Count(l => l.Contains("<- failed")).Should().Be(1, message);
        var first = lines.IndexOf("1. Increment");
        var second = lines.IndexOf("2. Increment");
        var third = lines.IndexOf("3. Increment");
        var fourth = lines.IndexOf("4. Increment <- failed");
        first.Should().BeLessThan(second);
        second.Should().BeLessThan(third);
        third.Should().BeLessThan(fourth);
    }
}
