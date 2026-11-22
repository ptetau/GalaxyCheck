using GalaxyCheck.Gens;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

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
        HealthCheck[]? SuppressHealthChecks = null);

    internal interface IPropertyRunner
    {
        Task Run(PropertyRunParameters parameters, ITestOutputHelper testOutputHelper);
    }
}
