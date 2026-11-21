using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace GalaxyCheck.Xunit.CodeAnalysis.Tests.Analyzers
{
    public class Verifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            return CSharpCodeFixVerifier<TAnalyzer, EmptyCodeFixProvider, DefaultVerifier>.Diagnostic(diagnosticId);
        }

        public static Task Verify(
            string[] sources,
            params DiagnosticResult[] diagnostics)
        {
            var test = new CSharpCodeFixTest<TAnalyzer, EmptyCodeFixProvider, DefaultVerifier>()
            {
                ReferenceAssemblies = new ReferenceAssemblies("net10.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0"), System.IO.Path.Combine("ref", "net10.0"))
            };

            foreach (var source in sources)
                test.TestState.Sources.Add(source);

            test.TestState.ExpectedDiagnostics.AddRange(diagnostics);

            test.TestState.AdditionalReferences.AddRange(new[]
            {
                MetadataReference.CreateFromFile(Assembly.GetAssembly(typeof(FactAttribute))!.Location),
                MetadataReference.CreateFromFile(Assembly.GetAssembly(typeof(IGen))!.Location),
                MetadataReference.CreateFromFile(Assembly.GetAssembly(typeof(PropertyAttribute))!.Location)
            });

            return test.RunAsync();
        }
    }
}
