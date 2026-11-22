using System;
using System.Collections.Generic;
using GalaxyCheck.Gens.Parameters;

namespace GalaxyCheck.Runners.Check
{
    public record CheckResult<T>(
        int Iterations,
        int Discards,
        int Shrinks,
        Counterexample<T>? Counterexample,
        IReadOnlyCollection<CheckIteration<T>> Checks,
        GenParameters InitialParameters,
        GenParameters NextParameters,
        TerminationReason TerminationReason)
    {
        public bool Falsified => Counterexample != null;

        /// <summary>The share of test cases with each label, as a percentage.</summary>
        [System.Runtime.Serialization.IgnoreDataMember]
        public System.Collections.Generic.IReadOnlyDictionary<string, double> Labels =>
            LabelShares ?? EmptyLabels;

        /// <summary>True when the run tried each value of the input space one time.</summary>
        [System.Runtime.Serialization.IgnoreDataMember]
        public bool Exhaustive => IsExhaustive;

        /// <summary>The text of the run.</summary>
        [System.Runtime.Serialization.IgnoreDataMember]
        public string Report => ReportBuilder.Build(this);

        private static readonly System.Collections.Generic.IReadOnlyDictionary<string, double> EmptyLabels =
            new System.Collections.Generic.Dictionary<string, double>();

        internal System.Collections.Generic.IReadOnlyDictionary<string, double>? LabelShares { get; init; }

        internal bool IsExhaustive { get; init; }

        /// <summary>The count of the values of the input space of an exhaustive run.</summary>
        internal long ExhaustiveCount { get; init; }

        protected virtual bool PrintMembers(System.Text.StringBuilder builder)
        {
            builder.Append("Iterations = ").Append(Iterations).Append(", Falsified = ").Append(Falsified);
            return true;
        }

        /// <summary>The number of choices that the generators made.</summary>
        public int RandomnessConsumption { get; init; }
    }

    public record CheckIteration<T>(
        IReadOnlyList<object?> PresentedInput,
        T Value,
        GenParameters Parameters,
        Exception? Exception)
    {
        public bool IsCounterexample => Exception != null;
    }

    public record Counterexample<T>(
        T Value,
        GenParameters ReplayParameters,
        string Replay,
        Exception? Exception,
        IReadOnlyList<object?> PresentedInput);

    public enum TerminationReason
    {
        IsReplay = 1,
        DeepCheckDisabled = 2,
        ReachedMaximumIterations = 3,
        ReachedMaximumSize = 4,
        FoundTheoreticalSmallestCounterexample = 5,
        FoundPragmaticSmallestCounterexample = 6,
        FoundError = 7,
        ReachedShrinkLimit = 8
    }
}

namespace GalaxyCheck.Runners
{
    using GalaxyCheck.Runners.Check;

    internal static class ReportBuilder
    {
        /// <summary>The lines that list each label with its share.</summary>
        public static System.Collections.Generic.IReadOnlyList<string> LabelLines(
            System.Collections.Generic.IReadOnlyDictionary<string, double>? shares)
        {
            var lines = new System.Collections.Generic.List<string>();
            if (shares == null) return lines;

            foreach (var pair in System.Linq.Enumerable.OrderByDescending(shares, p => p.Value))
            {
                lines.Add($"{PercentFormat.Format(pair.Value)} {pair.Key}");
            }

            return lines;
        }

        public static string Build<T>(CheckResult<T> result)
        {
            var labelLines = LabelLines(result.LabelShares);

            if (result.Counterexample != null)
            {
                var counterexample = result.Counterexample;
                return PropertyFailedException.BuildReport(
                    new Counterexample<object?>(
                        counterexample.Value,
                        counterexample.ReplayParameters,
                        counterexample.Replay,
                        counterexample.Exception,
                        counterexample.PresentedInput),
                    result.Iterations,
                    result.Shrinks,
                    labelLines);
            }

            var lines = new System.Collections.Generic.List<string>
            {
                result.Iterations == 1 ? "Passed after 1 test" : $"Passed after {result.Iterations} tests"
            };

            if (result.IsExhaustive)
            {
                lines.Add($"Exhaustive: the run was exhaustive, and each of the {result.ExhaustiveCount} possible inputs ran one time");
            }

            if (labelLines.Count > 0)
            {
                lines.Add("Labels:");
                foreach (var line in labelLines)
                {
                    lines.Add("    " + line);
                }
            }

            return string.Join(System.Environment.NewLine, lines);
        }
    }
}
