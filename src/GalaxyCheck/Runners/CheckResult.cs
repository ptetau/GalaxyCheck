using System;
using System.Collections.Generic;
using GalaxyCheck.Gens.Parameters;

namespace GalaxyCheck.Runners.Check
{
    /// <summary>The result of a run of a property.</summary>
    /// <param name="Iterations">The count of test cases that ran.</param>
    /// <param name="Discards">The count of test cases that a precondition or a filter discarded.</param>
    /// <param name="Shrinks">The count of shrinks of the counterexample.</param>
    /// <param name="Counterexample">The shrunk counterexample, or null when the property held.</param>
    /// <param name="Checks">Each test case of the run, when the run recorded them.</param>
    /// <param name="InitialParameters">The seed and size of the first test case.</param>
    /// <param name="NextParameters">The seed and size that a next run would start from.</param>
    /// <param name="TerminationReason">Why the run stopped.</param>
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
        /// <summary>True when the run found a counterexample, or when a cover rule was not met.</summary>
        public bool Falsified => Counterexample != null || CoverageFailure != null;

        /// <summary>
        /// The cover rule that the run did not meet, or null. A coverage failure has no counterexample: no test case
        /// failed, and the run as a whole did not reach the share of a label.
        /// </summary>
        [System.Runtime.Serialization.IgnoreDataMember]
        public CoverageFailure? CoverageFailure { get; init; }

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

        /// <summary>Writes the iterations and whether the run falsified the property.</summary>
        protected virtual bool PrintMembers(System.Text.StringBuilder builder)
        {
            builder.Append("Iterations = ").Append(Iterations).Append(", Falsified = ").Append(Falsified);
            return true;
        }

        /// <summary>The number of choices that the generators made.</summary>
        public int RandomnessConsumption { get; init; }
    }

    /// <summary>One test case of a run.</summary>
    /// <param name="PresentedInput">The input values as a report shows them.</param>
    /// <param name="Value">The generated input.</param>
    /// <param name="Parameters">The seed and size of the test case.</param>
    /// <param name="Exception">The exception of the test case, or null when it passed.</param>
    public record CheckIteration<T>(
        IReadOnlyList<object?> PresentedInput,
        T Value,
        GenParameters Parameters,
        Exception? Exception)
    {
        /// <summary>True when the test case failed.</summary>
        public bool IsCounterexample => Exception != null;
    }

    /// <summary>A cover rule that a run did not meet.</summary>
    /// <param name="Label">The label of the rule.</param>
    /// <param name="RequiredPercent">The share of the test cases that the rule requires, as a percentage.</param>
    /// <param name="ActualPercent">The share of the test cases that had the label, as a percentage.</param>
    /// <param name="Message">The text that states the label, the required share, and the share that was reached.</param>
    public record CoverageFailure(
        string Label,
        double RequiredPercent,
        double ActualPercent,
        string Message);

    /// <summary>An input for which the property fails, after shrinking.</summary>
    /// <param name="Value">The input value.</param>
    /// <param name="ReplayParameters">The seed and size that generate the value.</param>
    /// <param name="Replay">The replay string. Pass it as the replay argument, or in [Replay], to run this test case again.</param>
    /// <param name="Exception">The exception of the test function, or null when it returned false.</param>
    /// <param name="PresentedInput">The input values as a report shows them, one for each parameter.</param>
    public record Counterexample<T>(
        T Value,
        GenParameters ReplayParameters,
        string Replay,
        Exception? Exception,
        IReadOnlyList<object?> PresentedInput);

    /// <summary>Why a run stopped.</summary>
    public enum TerminationReason
    {
        /// <summary>The run replayed one test case.</summary>
        IsReplay = 1,
        /// <summary>The run stopped at the first counterexample, as deep checking was off.</summary>
        DeepCheckDisabled = 2,
        /// <summary>The run ran each iteration.</summary>
        ReachedMaximumIterations = 3,
        /// <summary>The run reached the largest size.</summary>
        ReachedMaximumSize = 4,
        /// <summary>The run found a counterexample that cannot shrink further.</summary>
        FoundTheoreticalSmallestCounterexample = 5,
        /// <summary>The run found a counterexample that is small enough.</summary>
        FoundPragmaticSmallestCounterexample = 6,
        /// <summary>The test function threw an error that ends the run.</summary>
        FoundError = 7,
        /// <summary>Shrinking stopped at the shrink limit.</summary>
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

            if (result.CoverageFailure != null)
            {
                return PropertyFailedException.BuildCoverageReport(result.CoverageFailure, result.Iterations, labelLines);
            }

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
