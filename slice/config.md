# slice config

- stack: C# on .NET 10 and .NET 11, xunit 2.9.3, xunit v3, TUnit, NUnit, MSTest, FluentAssertions 7.2.2
- test command: `dotnet test src/GalaxyCheck.Tests.Slice --logger "junit;LogFilePath={junit}"`
- result format: junit
- tests folder: `src/GalaxyCheck.Tests.Slice`
- source folders: `src/GalaxyCheck, src/GalaxyCheck.Xunit, src/GalaxyCheck.Xunit.V3, src/GalaxyCheck.TUnit, src/GalaxyCheck.NUnit, src/GalaxyCheck.Benchmarks, src/GalaxyCheck.MSTest, src/GalaxyCheck.SourceGeneration, docs`
- source files: `.github/workflows/dotnet.yml, GalaxyCheck.sln, nuget.config, README.md, packages-local/**, src/GalaxyCheck.Tests/GalaxyCheck.Tests.csproj, src/GalaxyCheck.Tests.V3/GalaxyCheck.Tests.V3.csproj, src/GalaxyCheck.Xunit.Tests/GalaxyCheck.Xunit.Tests.csproj, src/GalaxyCheck.Xunit.CodeAnalysis.Tests/GalaxyCheck.Xunit.CodeAnalysis.Tests.csproj, samples/GalaxyCheck.Samples.EverythingIsSerializable/GalaxyCheck.Samples.EverythingIsSerializable.csproj`
- security scan: none

A `junit` command writes its report to `{junit}`.
Each test name holds the spec number, the ID of its slice, and the requirement: `test_NNN_Sn_Rn_<what>`, for example `test_001_S1_R2_covers_full_range`.
The tests folder is one xunit project. It references the two library projects and the `JunitXml.TestLogger` package.
