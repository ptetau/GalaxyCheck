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

        public static readonly DiagnosticDescriptor ReflectionCall = new DiagnosticDescriptor(
            id: "GCSG002",
            title: "The call uses reflection at run time",
            messageFormat:
                "This call uses reflection at run time and does not work with trimming or NativeAOT: {0}",
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
                    predicate: static (node, _) => IsCandidate(node),
                    transform: static (syntaxContext, cancellationToken) =>
                        Plan((InvocationExpressionSyntax)syntaxContext.Node, syntaxContext.SemanticModel, cancellationToken))
                .Where(static request => request is { Code: not null })
                .Select(static (request, _) => request!)
                .Collect();

            var canRegister = context.CompilationProvider.Select(static (compilation, _) =>
                compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute") != null &&
                compilation.GetTypeByMetadataName("GalaxyCheck.Gens.Generated.GeneratedGens") != null);

            context.RegisterSourceOutput(requests.Combine(canRegister), static (output, input) => Emit(output, input.Left, input.Right));
        }

        internal static bool IsCandidate(SyntaxNode node) =>
            node is InvocationExpressionSyntax invocation &&
            invocation.Expression switch
            {
                MemberAccessExpressionSyntax { Name: GenericNameSyntax name } => IsCreateName(name, invocation) || IsOverrideName(name),
                MemberAccessExpressionSyntax { Name: IdentifierNameSyntax name } => name.Identifier.ValueText == "OverrideMember",
                GenericNameSyntax name => IsCreateName(name, invocation) || IsOverrideName(name),
                IdentifierNameSyntax name => name.Identifier.ValueText == "OverrideMember",
                _ => false
            };

        private static bool IsCreateName(GenericNameSyntax name, InvocationExpressionSyntax invocation) =>
            name.Identifier.ValueText == "Create" && name.TypeArgumentList.Arguments.Count == 1 &&
            invocation.ArgumentList.Arguments.Count <= 1;

        private static bool IsOverrideName(GenericNameSyntax name) => name.Identifier.ValueText == "OverrideMember";

        /// <summary>
        /// Plans one call: generated code, GCSG001 (no generated code for the type), or GCSG002 (the call always
        /// uses reflection). The analyzer reports the diagnostics, so that a pragma or an editorconfig can change them.
        /// </summary>
        internal static CreateRequest? Plan(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken cancellationToken)
        {
            if (model.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
            {
                return null;
            }

            var containingType = method.ContainingType?.OriginalDefinition.ToDisplayString();

            if (method.Name == "OverrideMember" && containingType == "GalaxyCheck.Gens.IReflectedGen<T>")
            {
                return CreateRequest.Reflection(
                    "OverrideMember always builds the generator by reflection; write the generator with Gen.Select or Gen.Zip instead");
            }

            if (method.Name != "Create" || method.TypeArguments.Length != 1 || method.Parameters.Length != 1)
            {
                return null;
            }

            bool isGenCreate;
            if (containingType == "GalaxyCheck.Gen")
            {
                isGenCreate = true;
            }
            else if (containingType == "GalaxyCheck.Gens.IGenFactory")
            {
                isGenCreate = IsDefaultFactory(invocation, model, cancellationToken);
            }
            else
            {
                return null;
            }

            var type = method.TypeArguments[0];
            var typeName = type.ToDisplayString();
            if (ContainsTypeParameter(type))
            {
                // A type parameter has no type until run time, so no code can be written for it.
                return CreateRequest.Reflection(
                    $"the type argument '{typeName}' holds a type parameter, so GalaxyCheck reads the type by reflection");
            }

            if (!isGenCreate)
            {
                return CreateRequest.Reflection(
                    $"Create<{typeName}>() on a factory with a registration builds the generator by reflection");
            }

            if (invocation.ArgumentList.Arguments.Count == 1 && !IsNullConstant(
                    model.GetConstantValue(invocation.ArgumentList.Arguments[0].Expression, cancellationToken)))
            {
                return CreateRequest.Reflection(
                    $"Create<{typeName}>() with a NullabilityInfo builds the generator by reflection");
            }

            // The run time has one generator for each run-time type, and Address? and Address are one type.
            var rootType = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            var typeKey = rootType.ToDisplayString(TypeFormat);
            var writer = new GenWriter(model.Compilation);
            var expression = writer.Write(rootType, "$", isNullable: false, ImmutableHashSet<ITypeSymbol>.Empty.WithComparer(SymbolEqualityComparer.Default));

            return expression.Code != null
                ? new CreateRequest(typeKey, expression.Code, null, null, string.Join("|", writer.ComposedTypes), IsReflectionCall: false)
                : new CreateRequest(typeKey, null, typeName, expression.Reason, "", IsReflectionCall: false);
        }

        private static bool IsNullConstant(Optional<object?> constant) => constant.HasValue && constant.Value == null;

        /// <summary>True for <c>Gen.Factory().Create&lt;T&gt;()</c>: a factory with no registration uses the generated code.</summary>
        private static bool IsDefaultFactory(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken cancellationToken) =>
            invocation.Expression is MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax receiver } &&
            receiver.ArgumentList.Arguments.Count == 0 &&
            model.GetSymbolInfo(receiver, cancellationToken).Symbol is IMethodSymbol { Name: "Factory", Parameters.Length: 0 } factory &&
            factory.ContainingType?.ToDisplayString() == "GalaxyCheck.Gen";

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
            // The failure report reads the members and the constructor of a value. Keep them in a trimmed app.
            foreach (var composedType in generated
                         .SelectMany(r => r.ComposedTypes.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                         .Distinct()
                         .OrderBy(t => t, StringComparer.Ordinal))
            {
                source.Append("        [global::System.Diagnostics.CodeAnalysis.DynamicDependency(")
                    .Append("global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | ")
                    .Append("global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(")
                    .Append(composedType).AppendLine("))]");
            }

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
            private readonly SortedSet<string> _composedTypes = new SortedSet<string>(StringComparer.Ordinal);

            public GenWriter(Compilation compilation)
            {
                _compilation = compilation;
            }

            /// <summary>The types that the generated code makes by a constructor, as C# type names.</summary>
            public IEnumerable<string> ComposedTypes => _composedTypes;

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

                // The generated code names the type with no annotation: "new Address?(...)" does not compile.
                type = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);

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

                var blocked = BlockedReason(named);
                if (blocked != null)
                {
                    return GenExpression.Unsupported(blocked);
                }

                if (HasReferenceTypeArgument(named))
                {
                    // The run time has one generator for Box<string> and Box<string?>, and the reflection path reads
                    // the nullability of a member from the open type. The generated code cannot match both.
                    return GenExpression.Unsupported("a generic type with a reference type argument is not supported");
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
                if (IsBlockedMember(constructor))
                {
                    return GenExpression.Unsupported("the constructor is obsolete with an error, or experimental");
                }

                if (HasRequiredMember(named) && !constructor.GetAttributes().Any(a =>
                        a.AttributeClass?.ToDisplayString() == "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute"))
                {
                    return GenExpression.Unsupported("a type with a required member is not supported");
                }

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

                var typeName = named.ToDisplayString(TypeFormat);
                _composedTypes.Add(typeName);
                return GenExpression.Of(
                    $"{HelpersType}.Members({string.Join(", ", memberCodes)})" +
                    $".Select(static values => new {typeName}({string.Join(", ", casts)}))");
            }

            /// <summary>The reason that generated code cannot name the type, or null.</summary>
            private static string? BlockedReason(INamedTypeSymbol named)
            {
                for (var current = named; current != null; current = current.ContainingType)
                {
                    if (current.IsFileLocal)
                    {
                        return "a file-local type is not supported";
                    }

                    if (IsBlockedMember(current))
                    {
                        return "a type that is obsolete with an error, or experimental, is not supported";
                    }
                }

                return null;
            }

            /// <summary>True for a symbol whose use is a compile error: obsolete with an error, or experimental.</summary>
            private static bool IsBlockedMember(ISymbol symbol) => symbol.GetAttributes().Any(a =>
                (a.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute" &&
                 a.ConstructorArguments.Length >= 2 && a.ConstructorArguments[1].Value is true) ||
                a.AttributeClass?.ToDisplayString() == "System.Diagnostics.CodeAnalysis.ExperimentalAttribute");

            private static bool HasRequiredMember(INamedTypeSymbol named)
            {
                for (var current = named; current != null; current = current.BaseType)
                {
                    if (current.GetMembers().Any(m => m is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true }))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool HasReferenceTypeArgument(INamedTypeSymbol named)
            {
                for (var current = named; current != null; current = current.ContainingType)
                {
                    foreach (var argument in current.TypeArguments)
                    {
                        if (!argument.IsValueType ||
                            (argument is INamedTypeSymbol namedArgument && HasReferenceTypeArgument(namedArgument)))
                        {
                            return true;
                        }
                    }
                }

                return false;
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
        internal sealed record CreateRequest(
            string TypeKey,
            string? Code,
            string? TypeName,
            string? Reason,
            string ComposedTypes,
            bool IsReflectionCall)
        {
            /// <summary>A call that always takes the reflection path. It gets GCSG002 and no generated code.</summary>
            public static CreateRequest Reflection(string reason) =>
                new CreateRequest("", null, null, reason, "", IsReflectionCall: true);
        }
    }
}
