using GalaxyCheck.Gens;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GalaxyCheck.Internal
{
    internal record PropertyRunParameters(
        AsyncProperty Property,
        int Iterations,
        int ShrinkLimit,
        string? Replay,
        int? Seed,
        int? Size,
        TimeSpan? Timeout = null,
        bool Exhaustive = true,
        HealthCheck[]? SuppressHealthChecks = null,
        IReadOnlyList<PropertyExample>? Examples = null,
        SavedFailure? SavedFailure = null,
        Func<string?>? TakeFailureMemoryNote = null,
        FailedTestIdentity? Identity = null);

    internal interface IPropertyRunner
    {
        Task Run(PropertyRunParameters parameters);
    }
}
