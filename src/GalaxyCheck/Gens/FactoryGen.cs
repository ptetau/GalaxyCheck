using System;
using System.Reflection;

namespace GalaxyCheck
{
    using Gens;

    public static partial class Gen
    {
        /// <summary>
        /// Creates a factory for <see cref="IReflectedGen{T}"/>. The factory allows you to assign specific generators to
        /// types, and then be able to share that configuration across many auto-generators, see
        /// <see cref="IGenFactory"/>.
        /// </summary>
        /// <returns>A factory for auto-generators.</returns>
        public static IGenFactory Factory() => new GenFactory();
    }

    public static partial class Extensions
    {
        /// <summary>
        /// Creates an auto-generator for the given type, using the configuration that was specified on this factory.
        /// </summary>
        /// <returns>A generator for the given type.</returns>
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(Gens.Generated.ReflectionMessages.Path)]
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode(Gens.Generated.ReflectionMessages.Path)]
        public static IGen<object> Create(this IGenFactory factory, Type type, NullabilityInfo? nullabilityInfo = null)
        {
            var method = typeof(IGenFactory).GetMethod(nameof(IGenFactory.Create), BindingFlags.Public | BindingFlags.Instance)!;
            var genericMethod = method.MakeGenericMethod(type);
            return ((IGen)genericMethod.Invoke(obj: factory, new object?[] { nullabilityInfo })!).Cast<object>();
        }
    }
}

namespace GalaxyCheck.Gens
{
    using Generated;
    using Internal;
    using ReflectedGenHelpers;
    using System.Diagnostics.CodeAnalysis;
    using GalaxyCheck.Internal;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Drawing;
    using System.Numerics;
    using System.Text;


    /// <summary>A generator that builds values of <typeparamref name="T"/> from their constructors and members.</summary>
    public interface IReflectedGen<T> : IGen<T>
    {
        /// <summary>Uses the given generator for one member of the value.</summary>
        IReflectedGen<T> OverrideMember<TMember>(
            Expression<Func<T, TMember>> memberSelector,
            IGen<TMember> memberGen);
    }

    /// <summary>Builds generators for types, with registered generators and implementations. Create one with <c>Gen.Factory()</c>.</summary>
    public interface IGenFactory
    {
        /// <summary>
        /// Registers a custom generator to the given type. The generator is applied to all <see cref="IReflectedGen{T}"/>
        /// that are created by this factory.
        /// </summary>
        /// <param name="type">The type to register the generator against.</param>
        /// <param name="gen">The generator to register at the type.</param>
        /// <returns>A new factory with the registration applied.</returns>
        IGenFactory RegisterType(Type type, IGen gen);

        /// <summary>Sets the concrete types that the factory generates for an interface or an abstract class.</summary>
        IGenFactory RegisterImplementations<T>(params Type[] candidateTypes) => throw new System.NotImplementedException("not implemented");

        /// <summary>
        /// Registers a custom generator to the given type. The generator is applied to all <see cref="IReflectedGen{T}"/>
        /// that are created by this factory.
        /// </summary>
        /// <param name="gen">The generator to register at the type.</param>
        /// <returns>A new factory with the registration applied.</returns>
        IGenFactory RegisterType<T>(IGen<T> gen);

        /// <summary>
        /// Registers a custom generator to the given type. The generator is applied to all
        /// <see cref="IReflectedGen{T}"/>. The registration function can reference existing conventions defined by the
        /// factory. Note, only conventions previously defined are used.
        /// </summary>
        /// <param name="type">The type to register the generator against.</param>
        /// <param name="genFunc">A function that takes the current factory and returns a generator. The factory can be
        /// used to create the gen, but it only uses conventions that were previously defined.</param>
        /// <returns>A new factory with the registration applied.</returns>
        IGenFactory RegisterType(Type type, Func<IGenFactory, IGen> genFunc);

        /// <summary>
        /// Registers a custom generator to the given type. The generator is applied to all
        /// <see cref="IReflectedGen{T}"/>. The registration function can reference existing conventions defined by the
        /// factory. Note, only conventions previously defined are used.
        /// </summary>
        /// <param name="genFunc">A function that takes the current factory and returns a generator. The factory can be
        /// used to create the gen, but it only uses conventions that were previously defined.</param>
        /// <returns>A new factory with the registration applied.</returns>
        IGenFactory RegisterType<T>(Func<IGenFactory, IGen<T>> genFunc);

        /// <summary>
        /// Creates an auto-generator for the given type, using the configuration that was specified on this factory.
        /// </summary>
        /// <returns>A generator for the given type.</returns>
        IReflectedGen<T> Create<T>(NullabilityInfo? nullabilityInfo = null) where T : notnull;
    }

    internal class GenFactory : IGenFactory
    {
        private static readonly IReadOnlyDictionary<Type, Func<IGen>> DefaultRegisteredGensByType =
            new Dictionary<Type, Func<IGen>>
            {
                // Integers
                { typeof(sbyte), () => Gen.SByte() },
                { typeof(byte), () => Gen.Byte() },
                { typeof(short), () => Gen.Int16() },
                { typeof(ushort), () => Gen.UInt16() },
                { typeof(int), () => Gen.Int32() },
                { typeof(uint), () => Gen.UInt32() },
                { typeof(long), () => Gen.Int64() },
                { typeof(ulong), () => Gen.UInt64() },
                { typeof(nint), () => Gen.IntPtr() },
                { typeof(nuint), () => Gen.UIntPtr() },
                { typeof(Int128), () => Gen.Int128() },
                { typeof(UInt128), () => Gen.UInt128() },
                { typeof(BigInteger), () => Gen.BigInteger() },

                // Dates and times
                { typeof(DateTime), () => Gen.DateTime() },
                { typeof(DateTimeOffset), () => Gen.DateTimeOffset() },
                { typeof(TimeSpan), () => Gen.TimeSpan() },
                { typeof(DateOnly), () => Gen.DateOnly() },
                { typeof(TimeOnly), () => Gen.TimeOnly() },

                // Real numbers
                { typeof(Half), () => Gen.Half() },
                { typeof(float), () => Gen.Single() },
                { typeof(double), () => Gen.Double() },
                { typeof(decimal), () => Gen.Decimal() },
                { typeof(Complex), () => Gen.Complex() },
#if NET11_0_OR_GREATER
                { typeof(Decimal32), () => Gen.Decimal32() },
                { typeof(Decimal64), () => Gen.Decimal64() },
                { typeof(Decimal128), () => Gen.Decimal128() },
                { typeof(BFloat16), () => Gen.BFloat16() },
#endif

                // Misc...
                { typeof(Rune), () => Gen.Rune() },
                { typeof(char), () => Gen.Char() },
                { typeof(string), () => Gen.String() },
                { typeof(Guid), () => Gen.Guid() },
                { typeof(bool), () => Gen.Boolean() },
                { typeof(Color), () => Gen.Int32().Select(x => Color.FromArgb(x)) },
                { typeof(Uri), () => Gen.Uri() },
                { typeof(Version), () => Gen.Version() }
            };

        private readonly ImmutableDictionary<Type, Func<IGen>> _registeredGensByType;

        private readonly ImmutableDictionary<Type, ImmutableArray<Type>> _candidateTypesByType;

        private GenFactory(
            ImmutableDictionary<Type, Func<IGen>> registeredGensByType,
            ImmutableDictionary<Type, ImmutableArray<Type>> candidateTypesByType)
        {
            _registeredGensByType = registeredGensByType;
            _candidateTypesByType = candidateTypesByType;
        }

        public GenFactory() : this(
            DefaultRegisteredGensByType.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value),
            ImmutableDictionary<Type, ImmutableArray<Type>>.Empty)
        {
            _isDefault = true;
        }

        /// <summary>True for a factory with no registration of its own. Only it uses the generated code.</summary>
        private readonly bool _isDefault;

        IGenFactory IGenFactory.RegisterType(Type type, IGen gen) => RegisterType(type, (_) => gen);

        IGenFactory IGenFactory.RegisterImplementations<T>(params Type[] candidateTypes) =>
            new GenFactory(
                _registeredGensByType,
                _candidateTypesByType.SetItem(typeof(T), candidateTypes.ToImmutableArray()));

        IGenFactory IGenFactory.RegisterType<T>(IGen<T> gen) => RegisterType(typeof(T), (_) => gen);

        IGenFactory IGenFactory.RegisterType(Type type, Func<IGenFactory, IGen> genFunc) => RegisterType(type, genFunc);

        IGenFactory IGenFactory.RegisterType<T>(Func<IGenFactory, IGen<T>> genFunc) => RegisterType(typeof(T), genFunc);

        //// TODO: Cache this call by (this/T)
        IReflectedGen<T> IGenFactory.Create<T>(NullabilityInfo? nullabilityInfo) =>
            new ReflectedGen<T>(_registeredGensByType, _candidateTypesByType, nullabilityInfo)
            {
                UsesGeneratedCode = _isDefault
            };

        private IGenFactory RegisterType(Type type, Func<IGenFactory, IGen> genFunc) =>
            new GenFactory(_registeredGensByType.SetItem(type, () => genFunc(this)), _candidateTypesByType);
    }

    internal record ReflectedGenMemberOverride(string Path, IGen Gen);

    internal record ReflectedGen<T>(
        IReadOnlyDictionary<Type, Func<IGen>> RegisteredGensByType,
        ImmutableDictionary<Type, ImmutableArray<Type>> CandidateTypesByType,
        ImmutableList<ReflectedGenMemberOverride> MemberOverrides,
        ReflectedGen<T>.OverrideMemberErrorKind? OverrideMemberError,
        NullabilityInfo? NullabilityInfo) : GenProvider<T>, IReflectedGen<T>
    {
        public record OverrideMemberErrorKind
        {
            private OverrideMemberErrorKind()
            {
            }

            public record InvalidExpression(string Expression) : OverrideMemberErrorKind;

            public record AttemptedOverrideOnRegisteredGen(string Expression) : OverrideMemberErrorKind;
        }

        public ReflectedGen(
            IReadOnlyDictionary<Type, Func<IGen>> registeredGensByType,
            NullabilityInfo? nullabilityInfo)
            : this(
                registeredGensByType,
                ImmutableDictionary<Type, ImmutableArray<Type>>.Empty,
                nullabilityInfo)
        {
        }

        public ReflectedGen(
            IReadOnlyDictionary<Type, Func<IGen>> registeredGensByType,
            ImmutableDictionary<Type, ImmutableArray<Type>> candidateTypesByType,
            NullabilityInfo? nullabilityInfo)
            : this(
                registeredGensByType,
                candidateTypesByType,
                ImmutableList.Create<ReflectedGenMemberOverride>(),
                null,
                nullabilityInfo)
        {
        }

        public IReflectedGen<T> OverrideMember<TMember>(Expression<Func<T, TMember>> memberSelector,
            IGen<TMember> fieldGen)
        {
            // TODO: Either.Select, Either.SelectMany, Either.SelectError
            var pathResult = PathResolver.FromExpression(memberSelector);
            return pathResult.Match<(string path, string expression), string, IReflectedGen<T>>(
                fromLeft: x => RegisteredGensByType.ContainsKey(typeof(T))
                    ? this with
                    {
                        OverrideMemberError = new OverrideMemberErrorKind.AttemptedOverrideOnRegisteredGen(x.expression)
                    }
                    : this with
                    {
                        MemberOverrides = MemberOverrides.Add(new ReflectedGenMemberOverride(x.path, fieldGen))
                    },
                fromRight: expression => this with
                {
                    OverrideMemberError = new OverrideMemberErrorKind.InvalidExpression(expression)
                });
        }

        /// <summary>
        /// True when the generator may use the code of the source generator: it comes from the default factory, and
        /// it has no member override and no nullability of its own.
        /// </summary>
        public bool UsesGeneratedCode { get; init; }

        protected override IGen<T> Get
        {
            get
            {
                if (OverrideMemberError != null)
                {
                    return ResolveError(OverrideMemberError);
                }

                if (UsesGeneratedCode && MemberOverrides.IsEmpty && NullabilityInfo == null &&
                    GeneratedGens.TryGet<T>(out var generatedGen))
                {
                    return generatedGen;
                }

                return BuildByReflection();
            }
        }

        // The source generator reports a diagnostic at compile time for each call that takes this path: GCSG001 for a
        // Gen.Create<T>() that it cannot write, and GCSG002 for a type parameter, a factory with a registration,
        // OverrideMember, and a NullabilityInfo argument. So a trimmed or NativeAOT app learns there that a call
        // takes this path.
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The source generator reports GCSG001 or GCSG002 for each call that takes the reflection path.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The source generator reports GCSG001 or GCSG002 for each call that takes the reflection path.")]
        private IGen<T> BuildByReflection()
        {
            var context = ReflectedGenHandlerContext.Create(typeof(T), NullabilityInfo);

            return ReflectedGenBuilder
                .Build(typeof(T), RegisteredGensByType, MemberOverrides, CandidateTypesByType, context)
                .Cast<T>();
        }

        private static IGen<T> ResolveError(OverrideMemberErrorKind overrideMemberError) =>
            overrideMemberError switch
            {
                OverrideMemberErrorKind.InvalidExpression invalidExpression =>
                    Error(
                        $"expression '{invalidExpression.Expression}' was invalid, an overriding expression may only contain member access"),
                OverrideMemberErrorKind.AttemptedOverrideOnRegisteredGen
                    attemptedOverrideOnRegisteredGen =>
                    Error(
                        $"attempted to override expression '{attemptedOverrideOnRegisteredGen.Expression}' on type '{typeof(T)}', but overriding members for registered types is not currently supported (GitHub issue: https://github.com/nth-commit/GalaxyCheck/issues/346)"),
                _ => throw new NotSupportedException($"Type not supported in switch: {overrideMemberError.GetType()}")
            };

        private static IGen<T> Error(string message) => Gen.Advanced.Error<T>(message);
    }
}
