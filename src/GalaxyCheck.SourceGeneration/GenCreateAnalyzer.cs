using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GalaxyCheck.SourceGeneration
{
    /// <summary>
    /// Reports GCSG001 for a <c>Gen.Create&lt;T&gt;()</c> that the generator cannot write, and GCSG002 for a call that
    /// always uses reflection at run time. It shares the plan of <see cref="GenCreateGenerator"/>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class GenCreateAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(GenCreateGenerator.UnsupportedType, GenCreateGenerator.ReflectionCall);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (!GenCreateGenerator.IsCandidate(context.Node))
            {
                return;
            }

            var request = GenCreateGenerator.Plan((InvocationExpressionSyntax)context.Node, context.SemanticModel, context.CancellationToken);
            if (request == null || request.Code != null)
            {
                return;
            }

            var location = context.Node.GetLocation();
            context.ReportDiagnostic(request.IsReflectionCall
                ? Diagnostic.Create(GenCreateGenerator.ReflectionCall, location, request.Reason)
                : Diagnostic.Create(GenCreateGenerator.UnsupportedType, location, request.TypeName, request.Reason));
        }
    }
}
