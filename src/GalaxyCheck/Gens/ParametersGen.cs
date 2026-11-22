namespace GalaxyCheck
{
    using GalaxyCheck.Gens;
    using System.Collections.Generic;
    using System.Reflection;

    public static partial class Gen
    {
        private static readonly IReadOnlyDictionary<int, IGen> DefaultGensByParameterIndex = new Dictionary<int, IGen>();

        /// <summary>
        /// Creates a generator that produces parameters to the given method. Can be used to dynamically invoke a
        /// method or a delegate.
        /// </summary>
        /// <param name="methodInfo">The method to generate parameters for.</param>
        /// <param name="genFactory">The <see cref="IGenFactory"/> to use to create generators from parameter
        /// types. If null, it uses the default factory.</param>
        /// <param name="customGens">The generator to use, keyed by the index of the parameter. Generators
        /// supplied through this collection will not be associated with <paramref name="genFactory"/>.</param>
        /// <returns>The new generator.</returns>
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(GalaxyCheck.Gens.Generated.ReflectionMessages.Path)]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode(GalaxyCheck.Gens.Generated.ReflectionMessages.Path)]
        public static IGen<object[]> Parameters(
            MethodInfo methodInfo,
            IGenFactory? genFactory = null,
            IReadOnlyDictionary<int, IGen>? customGens = null) =>
                new ParametersGen(methodInfo, genFactory ?? Factory(), customGens ?? DefaultGensByParameterIndex);
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Internal;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(GalaxyCheck.Gens.Generated.ReflectionMessages.Path)]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode(GalaxyCheck.Gens.Generated.ReflectionMessages.Path)]
    internal record ParametersGen : GenProvider<object[]>, IGen<object[]>
    {
        private readonly MethodInfo _methodInfo;
        private readonly IGenFactory _genFactory;
        private readonly IReadOnlyDictionary<int, IGen> _customGens;

        public ParametersGen(MethodInfo methodInfo, IGenFactory genFactory, IReadOnlyDictionary<int, IGen> customGens)
        {
            _methodInfo = methodInfo;
            _genFactory = genFactory;
            _customGens = customGens;
        }

        protected override IGen<object[]> Get => CreateGen(_methodInfo, _genFactory, _customGens);

        private GalaxyCheck.Properties.IFiniteGen?[]? _parts;

        /// <summary>
        /// The generator of each parameter that the run generates. The token is left out: the runner gives it.
        /// </summary>
        private GalaxyCheck.Properties.IFiniteGen?[] Parts => _parts ??= CreateParts();

        private GalaxyCheck.Properties.IFiniteGen?[] CreateParts()
        {
            var parameters = _methodInfo.GetParameters();
            if (_customGens.Keys.Any(index => index < 0 || index >= parameters.Length))
            {
                return new GalaxyCheck.Properties.IFiniteGen?[] { null };
            }

            var parts = new List<GalaxyCheck.Properties.IFiniteGen?>();
            for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
            {
                var customGen = _customGens.ContainsKey(parameterIndex) ? _customGens[parameterIndex] : null;
                if (IsToken(customGen, parameters[parameterIndex])) continue;
                parts.Add(ResolveGen(_genFactory, customGen, parameters[parameterIndex]).Advanced as GalaxyCheck.Properties.IFiniteGen);
            }

            return parts.ToArray();
        }

        /// <summary>
        /// The parameters are finite when each generated parameter is finite. The count is the product of their
        /// counts. A method with no generated parameter is not finite.
        /// </summary>
        public override long? Count(long limit)
        {
            var parts = Parts;
            return parts.Length == 0 ? null : GalaxyCheck.Properties.Finite.Product(parts, limit);
        }

        public override void WriteChoices(long index, List<ulong> choices) =>
            GalaxyCheck.Properties.Finite.WriteProduct(Parts!, index, choices);

        private static bool IsToken(IGen? customGen, ParameterInfo parameterInfo) =>
            customGen == null && parameterInfo.ParameterType == typeof(System.Threading.CancellationToken);

        private static IGen<object[]> CreateGen(
            MethodInfo methodInfo,
            IGenFactory genFactory,
            IReadOnlyDictionary<int, IGen> customGens)
        {
            var parameters = methodInfo.GetParameters();

            var invalidIndex = customGens.Keys.Except(Enumerable.Range(0, parameters.Length)).Cast<int?>().FirstOrDefault();
            if (invalidIndex is not null)
            {
                return Gen.Advanced.Error<object[]>($"parameter index '{invalidIndex.Value}' of custom generator was not valid");
            }

            var gens = parameters
                .Select((parameterInfo, parameterIndex) =>
                {
                    var customGen = customGens.ContainsKey(parameterIndex) ? customGens[parameterIndex] : null;
                    return ResolveGen(genFactory, customGen, parameterInfo).Advanced.ReferenceRngWaypoint(parameterIndex);
                });

            return Gen.Zip(gens).Select(xs => xs.ToArray());
        }

        private static IGen<object> ResolveGen(IGenFactory genFactory, IGen? customGen, ParameterInfo parameterInfo)
        {
            if (IsToken(customGen, parameterInfo))
            {
                // The token is not generated: the runner gives the token of the test case when it runs the method.
                return Gen.Constant<object>(System.Threading.CancellationToken.None);
            }

            if (customGen == null)
            {
                var configuredGenFactory = ConfigureGenFactory(genFactory, parameterInfo);

                var createGenMethodInfo = typeof(IGenFactory)
                    .GetMethod(nameof(IGenFactory.Create))!
                    .MakeGenericMethod(parameterInfo.ParameterType);

                var nullabilityInfoContext = new NullabilityInfoContext();
                var nullabilityInfo = nullabilityInfoContext.Create(parameterInfo);

                var gen = (IGen)createGenMethodInfo.Invoke(configuredGenFactory, new object[] { nullabilityInfo })!;

                return gen
                    .Cast<object>()
                    .SelectError(error => SelectParameterError(parameterInfo, error));
            }
            else
            {
                var genTypeArgument = customGen.ReflectGenTypeArgument();

                if (parameterInfo.ParameterType.IsAssignableFrom(genTypeArgument) == false)
                {
                    return Gen.Advanced.Error<object>(
                        $"generator of type '{genTypeArgument.FullName}' is not compatible with parameter of type '{parameterInfo.ParameterType.FullName}'");
                }

                return customGen.Cast<object>();
            }
        }

        private static IGenFactory ConfigureGenFactory(IGenFactory genFactory, ParameterInfo parameterInfo)
        {
            if (TryCreateGenFromAttribute(parameterInfo, out IGen? gen))
            {
                return genFactory.RegisterType(parameterInfo.ParameterType, gen!);
            }

            return genFactory;
        }

        private static bool TryCreateGenFromAttribute(ParameterInfo parameterInfo, out IGen? gen)
        {
            var genAttributes = parameterInfo
                .GetCustomAttributes()
                .OfType<GenAttribute>();

            var (genAttribute, otherGenAttributes) = genAttributes;

            if (genAttribute == null)
            {
                gen = null;
                return false;
            }

            if (otherGenAttributes.Any())
            {
                gen = Gen.Advanced.Error(
                    parameterInfo.ParameterType,
                    $"multiple {nameof(GenAttribute)}s is unsupported");
                return true;
            }

            gen = genAttribute.Value;
            return true;
        }

        private static string SelectParameterError(ParameterInfo parameterInfo, string error)
        {
            return $"unable to generate value for parameter '{parameterInfo.Name}', {error}";
        }
    }
}
