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
        public System.Collections.Generic.IReadOnlyDictionary<string, double> Labels => throw new System.NotImplementedException("not implemented");

        /// <summary>True when the run tried each value of the input space one time.</summary>
        [System.Runtime.Serialization.IgnoreDataMember]
        public bool Exhaustive => throw new System.NotImplementedException("not implemented");

        /// <summary>The text of the run.</summary>
        [System.Runtime.Serialization.IgnoreDataMember]
        public string Report => throw new System.NotImplementedException("not implemented");

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
