using Verifiable.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: ComVisible(false)]
[assembly: Guid("4e393276-7fd7-49d8-87bd-665a917cddce")]
[assembly: InternalsVisibleTo("GalaxyCheck.Tests")]
[assembly: InternalsVisibleTo("GalaxyCheck.Tests.V3")]
[assembly: InternalsVisibleTo("GalaxyCheck.Benchmarks")]
[assembly: Verifiable]
[assembly: System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
    "Trimming",
    "IL2026",
    Scope = "type",
    Target = "~T:GalaxyCheck.Gens.Parameters.BoundedSize.BoundedSizeJsonConverter",
    Justification = "Internal refined value object is never JSON-serialized via JsonSerializer.")]
[assembly: System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
    "Trimming",
    "IL2026",
    Scope = "type",
    Target = "~T:GalaxyCheck.Runners.Replaying.ReplayChoiceCount.ReplayChoiceCountJsonConverter",
    Justification = "Internal refined replay count is never JSON-serialized via JsonSerializer.")]
