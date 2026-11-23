using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GalaxyCheck.SourceGeneration
{
    /// <summary>
    /// Writes the code of <c>Gen.Create&lt;T&gt;()</c> for each type that a project asks for. The generated code
    /// registers each generator when the project loads, and it makes the same values as the reflection path. For a
    /// type that it cannot write, it reports <c>GCSG001</c>, and the call keeps the reflection path.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class GenCreateGenerator : IIncrementalGenerator
    {
        public static readonly DiagnosticDescriptor UnsupportedType = new DiagnosticDescriptor(
            id: "GCSG001",
            title: "Gen.Create<T>() has no generated code",
            messageFormat:
                "GalaxyCheck cannot generate the code of Gen.Create<{0}>(): {1}. The call uses the reflection path, which does not work in a trimmed or NativeAOT app.",
            category: "GalaxyCheck.SourceGeneration",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private const string GenType = "global::GalaxyCheck.Gen";
        private const string HelpersType = "global::GalaxyCheck.Gens.Generated.GeneratedGenHelpers";
        private const string RegistryType = "global::GalaxyCheck.Gens.Generated.GeneratedGens";

        private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
            .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var requests = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsCreateCandidate(node),
                    transform: static (syntaxContext, cancellationToken) => Plan(syntaxContext, cancellationToken))
                .Where(static request => request != null)
                .Select(static (request, _) => request!)
                .Collect();

            var canRegister = context.CompilationProvider.Select(static (compilation, _) =>
                compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute") != null &&
                compilation.GetTypeByMetadataName("GalaxyCheck.Gens.Generated.GeneratedGens") != null);

            context.RegisterSourceOutput(requests.Combine(canRegister), static (output, input) => Emit(output, input.Left, input.Right));
        }

        private static bool IsCreateCandidate(SyntaxNode node) =>
            node is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } invocation &&
            invocation.Expression switch
            {
                MemberAccessExpressionSyntax { Name: GenericNameSyntax name } => IsCreateName(name),
                GenericNameSyntax name => IsCreateName(name),
                _ => false
            };

        private static bool IsCreateName(GenericNameSyntax name) =>
            name.Identifier.ValueText == "Create" && name.TypeArgumentList.Arguments.Count == 1;

        private static CreateRequest? Plan(GeneratorSyntaxContext syntaxContext, CancellationToken cancellationToken)
        {
            var invocation = (InvocationExpressionSyntax)syntaxContext.Node;
            if (syntaxContext.SemanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method ||
                method.Name != "Create" ||
                method.TypeArguments.Length != 1 ||
                method.Parameters.Length != 1 ||
                method.ContainingType?.ToDisplayString() != "GalaxyCheck.Gen")
            {
                return null;
            }

            var type = method.TypeArguments[0];
            if (ContainsTypeParameter(type))
            {
                // A type parameter has no type until run time, so no code can be written for it.
                return null;
            }

            var location = LocationInfo.From(invocation.GetLocation());
            var typeKey = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(TypeFormat);
            var writer = new GenWriter(syntaxContext.SemanticModel.Compilation);
            var expression = writer.Write(type, "$", isNullable: false, ImmutableHashSet<ITypeSymbol>.Empty.WithComparer(SymbolEqualityComparer.Default));

            return expression.Code != null
                ? new CreateRequest(typeKey, expression.Code, null, null, location)
                : new CreateRequest(typeKey, null, type.ToDisplayString(), expression.Reason, location);
        }

        private static bool ContainsTypeParameter(ITypeSymbol type) => type switch
        {
            ITypeParameterSymbol => true,
            IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
            INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameter) ||
                                      (named.ContainingType != null && ContainsTypeParameter(named.ContainingType)),
            _ => false
        };

        private static void Emit(SourceProductionContext output, ImmutableArray<CreateRequest> requests, bool canRegister)
        {
            foreach (var request in requests.Where(r => r.Code == null))
            {
                output.ReportDiagnostic(Diagnostic.Create(UnsupportedType, request.Location.ToLocation(), request.TypeName, request.Reason));
            }

            var generated = requests
                .Where(r => r.Code != null)
                .GroupBy(r => r.TypeKey)
                .Select(g => g.First())
                .OrderBy(r => r.TypeKey, StringComparer.Ordinal)
                .ToList();

            if (!canRegister || generated.Count == 0)
            {
                return;
            }

            var source = new StringBuilder();
            source.AppendLine("// <auto-generated/>");
            source.AppendLine("#nullable enable");
            source.AppendLine("#pragma warning disable");
            source.AppendLine("namespace GalaxyCheck.SourceGeneration.Generated");
            source.AppendLine("{");
            source.AppendLine("    [global::System.CodeDom.Compiler.GeneratedCode(\"GalaxyCheck.SourceGeneration\", \"1.0.0\")]");
            source.AppendLine("    internal static class GalaxyCheckGeneratedGens");
            source.AppendLine("    {");
            source.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");
            source.AppendLine("        internal static void Register()");
            source.AppendLine("        {");
            foreach (var request in generated)
            {
                source.Append("            ").Append(RegistryType).Append(".Register<").Append(request.TypeKey)
                    .Append(">(static () => ").Append(request.Code).AppendLine(");");
            }

            source.AppendLine("        }");
            source.AppendLine("    }");
            source.AppendLine("}");

            output.AddSource("GalaxyCheck.GeneratedGens.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        }

        /// <summary>
        /// Writes the generator expression of a type. It follows the handlers of the reflection path in their order,
        /// so that both paths draw the same values.
        /// </summary>
        private sealed class GenWriter
        {
            private static readonly Dictionary<string, string> RegisteredGens = new Dictionary<string, string>
            {
                ["System.SByte"] = GenType + ".SByte()",
                ["System.Byte"] = GenType + ".Byte()",
                ["System.Int16"] = GenType + ".Int16()",
                ["System.UInt16"] = GenType + ".UInt16()",
                ["System.Int32"] = GenType + ".Int32()",
                ["System.UInt32"] = GenType + ".UInt32()",
                ["System.Int64"] = GenType + ".Int64()",
                ["System.UInt64"] = GenType + ".UInt64()",
                ["System.IntPtr"] = GenType + ".IntPtr()",
                ["System.UIntPtr"] = GenType + ".UIntPtr()",
                ["System.Int128"] = GenType + ".Int128()",
                ["System.UInt128"] = GenType + ".UInt128()",
                ["System.Numerics.BigInteger"] = GenType + ".BigInteger()",
                ["System.DateTime"] = GenType + ".DateTime()",
                ["System.DateTimeOffset"] = GenType + ".DateTimeOffset()",
                ["System.TimeSpan"] = GenType + ".TimeSpan()",
                ["System.DateOnly"] = GenType + ".DateOnly()",
                ["System.TimeOnly"] = GenType + ".TimeOnly()",
                ["System.Half"] = GenType + ".Half()",
                ["System.Single"] = GenType + ".Single()",
                ["System.Double"] = GenType + ".Double()",
                ["System.Decimal"] = GenType + ".Decimal()",
                ["System.Numerics.Complex"] = GenType + ".Complex()",
                ["System.Numerics.Decimal32"] = GenType + ".Decimal32()",
                ["System.Numerics.Decimal64"] = GenType + ".Decimal64()",
                ["System.Numerics.Decimal128"] = GenType + ".Decimal128()",
                ["System.Numerics.BFloat16"] = GenType + ".BFloat16()",
                ["System.Text.Rune"] = GenType + ".Rune()",
                ["System.Char"] = GenType + ".Char()",
                ["System.String"] = GenType + ".String()",
                ["System.Guid"] = GenType + ".Guid()",
                ["System.Boolean"] = GenType + ".Boolean()",
                ["System.Drawing.Color"] = GenType + ".Int32().Select(static x => global::System.Drawing.Color.FromArgb(x))",
                ["System.Uri"] = GenType + ".Uri()",
                ["System.Version"] = GenType + ".Version()",
            };

            private readonly Compilation _compilation;

            public GenWriter(Compilation compilation)
            {
                _compilation = compilation;
            }

            public GenExpression Write(ITypeSymbol type, string path, bool isNullable, ImmutableHashSet<ITypeSymbol> visiting)
            {
                if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableStruct)
                {
                    var inner = Write(nullableStruct.TypeArguments[0], path, isNullable: false, visiting);
                    return inner.Map(code => $"{GenType}.NullableStruct({code})");
                }

                if (isNullable && type.IsReferenceType)
                {
                    var inner = Write(type, path, isNullable: false, visiting);
                    return inner.Map(code => $"{GenType}.Nullable({code})");
                }

                if (RegisteredGens.TryGetValue(MetadataName(type), out var registered))
                {
                    return GenExpression.Of(registered);
                }

                if (!_compilation.IsSymbolAccessibleWithin(type, _compilation.Assembly))
                {
                    return GenExpression.Unsupported("the type is not accessible from the generated code");
                }

                if (type is IArrayTypeSymbol)
                {
                    return GenExpression.Unsupported("arrays are not supported");
                }

                if (type is not INamedTypeSymbol named)
                {
                    return GenExpression.Unsupported("only classes, records, structs, and enums are supported");
                }

                if (named.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.UnionAttribute"))
                {
                    return GenExpression.Unsupported("union types are not supported");
                }

                if (named.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable) ||
                    named.SpecialType == SpecialType.System_Collections_IEnumerable ||
                    named.IsTupleType ||
                    (named.ContainingNamespace?.ToDisplayString() == "System" && named.Name == "Tuple"))
                {
                    return GenExpression.Unsupported("collections, dictionaries, and tuples are not supported");
                }

                if (named.TypeKind == TypeKind.Enum)
                {
                    return GenExpression.Of($"{GenType}.Enum<{named.ToDisplayString(TypeFormat)}>()");
                }

                if (named.TypeKind == TypeKind.Interface || named.IsAbstract)
                {
                    return GenExpression.Unsupported("interfaces and abstract classes are not supported");
                }

                if (named.TypeKind != TypeKind.Class && named.TypeKind != TypeKind.Struct)
                {
                    return GenExpression.Unsupported("only classes, records, structs, and enums are supported");
                }

                // Reflection does not see the implicit parameterless constructor of a struct.
                var constructors = named.InstanceConstructors
                    .Where(c => c.DeclaredAccessibility == Accessibility.Public &&
                                !(named.TypeKind == TypeKind.Struct && c.IsImplicitlyDeclared && c.Parameters.Length == 0))
                    .ToList();

                if (constructors.Any(c => c.Parameters.Length == 0) || (named.TypeKind == TypeKind.Struct && constructors.Count == 0))
                {
                    return GenExpression.Unsupported("a type that is made by its parameterless constructor is not supported");
                }

                if (constructors.Count == 0)
                {
                    return GenExpression.Unsupported("the type has no public constructor");
                }

                var longest = constructors.Max(c => c.Parameters.Length);
                var candidates = constructors.Where(c => c.Parameters.Length == longest).ToList();
                if (candidates.Count > 1)
                {
                    return GenExpression.Unsupported("the type has more than one public constructor with the most parameters");
                }

                if (visiting.Contains(named))
                {
                    return GenExpression.Unsupported("a type that refers to itself is not supported");
                }

                var constructor = candidates[0];
                var nextVisiting = visiting.Add(named);
                var memberCodes = new List<string>();
                var casts = new List<string>();
                for (var i = 0; i < constructor.Parameters.Length; i++)
                {
                    var parameter = constructor.Parameters[i];
                    if (parameter.RefKind != RefKind.None || parameter.Type.IsRefLikeType || parameter.Type is IPointerTypeSymbol)
                    {
                        return GenExpression.Unsupported($"the parameter '{parameter.Name}' of the constructor is not supported");
                    }

                    var memberPath = path + "." + parameter.Name;
                    var memberIsNullable = parameter.Type.IsReferenceType && parameter.NullableAnnotation == NullableAnnotation.Annotated;
                    var member = Write(parameter.Type, memberPath, memberIsNullable, nextVisiting);
                    if (member.Code == null)
                    {
                        return GenExpression.Unsupported($"the parameter '{parameter.Name}' of type '{parameter.Type.ToDisplayString()}': {member.Reason}");
                    }

                    memberCodes.Add($"{HelpersType}.Member({member.Code}, {SymbolDisplay.FormatLiteral(memberPath, quote: true)})");
                    casts.Add($"({parameter.Type.ToDisplayString(TypeFormat)})values[{i}]");
                }

                return GenExpression.Of(
                    $"{HelpersType}.Members({string.Join(", ", memberCodes)})" +
                    $".Select(static values => new {named.ToDisplayString(TypeFormat)}({string.Join(", ", casts)}))");
            }

            private static string MetadataName(ITypeSymbol type) =>
                type is INamedTypeSymbol { ContainingType: null } named && !named.IsGenericType
                    ? $"{named.ContainingNamespace?.ToDisplayString()}.{named.MetadataName}"
                    : type.ToDisplayString();
        }

        private sealed class GenExpression
        {
            private GenExpression(string? code, string? reason)
            {
                Code = code;
                Reason = reason;
            }

            public string? Code { get; }

            public string? Reason { get; }

            public static GenExpression Of(string code) => new GenExpression(code, null);

            public static GenExpression Unsupported(string reason) => new GenExpression(null, reason);

            public GenExpression Map(Func<string, string> map) => Code == null ? this : Of(map(Code));
        }

        /// <summary>The plan of one call. It holds only strings, so that the incremental pipeline can compare it.</summary>
        private sealed record CreateRequest(string TypeKey, string? Code, string? TypeName, string? Reason, LocationInfo Location);

        private sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
        {
            public static LocationInfo From(Location location) =>
                new LocationInfo(location.SourceTree?.FilePath ?? "", location.SourceSpan, location.GetLineSpan().Span);

            public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);
        }
    }
}
