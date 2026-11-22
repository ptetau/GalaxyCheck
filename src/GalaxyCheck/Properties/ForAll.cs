using GalaxyCheck.Gens.Parameters;
using GalaxyCheck.Properties;
using System;
using System.Collections.Generic;

namespace GalaxyCheck
{
    public partial class Property
    {
        public static Property Nullary(Func<bool> func) => new Property(
            Gen.Constant(new object[] { }).Select(x => TestFactory.Create<object[]>(
                x,
                func,
                x)));

        public static Property<T0> ForAll<T0>(IGen<T0> gen0, Func<T0, bool> func) =>
            new Property<T0>(new ForAllGen<T0>(gen0, func, x => new object?[] { x }));

        public static Property<(T0, T1)> ForAll<T0, T1>(
            IGen<T0> gen0,
            IGen<T1> gen1,
            Func<T0, T1, bool> func) => new Property<(T0, T1)>(new ForAllGen<(T0, T1)>(
                Gen.Zip(gen0, gen1),
                tuple => func(tuple.Item1, tuple.Item2),
                tuple => new object?[] { tuple.Item1, tuple.Item2 }));

        public static Property<(T0, T1, T2)> ForAll<T0, T1, T2>(
            IGen<T0> gen0,
            IGen<T1> gen1,
            IGen<T2> gen2,
            Func<T0, T1, T2, bool> func) => new Property<(T0, T1, T2)>(new ForAllGen<(T0, T1, T2)>(
                Gen.Zip(gen0, gen1, gen2),
                tuple => func(tuple.Item1, tuple.Item2, tuple.Item3),
                tuple => new object?[] { tuple.Item1, tuple.Item2, tuple.Item3 }));

        public static Property<(T0, T1, T2, T3)> ForAll<T0, T1, T2, T3>(
            IGen<T0> gen0,
            IGen<T1> gen1,
            IGen<T2> gen2,
            IGen<T3> gen3,
            Func<T0, T1, T2, T3, bool> func) => new Property<(T0, T1, T2, T3)>(new ForAllGen<(T0, T1, T2, T3)>(
                Gen.Zip(gen0, gen1, gen2, gen3),
                tuple => func(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4),
                tuple => new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4 }));

        public static Property Nullary(Action func) => Nullary(func.AsTrueFunc());

        public static Property<T0> ForAll<T0>(IGen<T0> gen0, Action<T0> action) =>
            ForAll(gen0, action.AsTrueFunc());

        public static Property<(T0, T1)> ForAll<T0, T1>(
            IGen<T0> gen0,
            IGen<T1> gen1,
            Action<T0, T1> action) => ForAll(gen0, gen1, action.AsTrueFunc());

        public static Property<(T0, T1, T2)> ForAll<T0, T1, T2>(
            IGen<T0> gen0,
            IGen<T1> gen1,
            IGen<T2> gen2,
            Action<T0, T1, T2> action) => ForAll(gen0, gen1, gen2, action.AsTrueFunc());

        public static Property<(T0, T1, T2, T3)> ForAll<T0, T1, T2, T3>(
            IGen<T0> gen0,
            IGen<T1> gen1,
            IGen<T2> gen2,
            IGen<T3> gen3,
            Action<T0, T1, T2, T3> action) => ForAll(gen0, gen1, gen2, gen3, action.AsTrueFunc());
    }
}

namespace GalaxyCheck.Properties
{
    /// <summary>
    /// A property's test generator that the runner can also run directly, with no test object for each value.
    /// </summary>
    internal interface IFastProperty<out T>
    {
        /// <summary>
        /// Generates the input and runs the test function on it. An exception of the generator is thrown; an
        /// exception of the test function is a failure. A false precondition gives no failure and the exception
        /// <see cref="PreconditionFailedException"/>, and the runner discards the test case. When trackTime is set,
        /// generatedAt is the tick count after the generation.
        /// </summary>
        T Run(GenSource source, bool trackTime, out long generatedAt, out bool failed, out Exception? exception);

        IReadOnlyList<object?> Present(object? input);
    }

    internal sealed record ForAllGen<T>(
        IGen<T> Input,
        Func<T, bool> Func,
        Func<T, IReadOnlyList<object?>> Presenter) : Gens.Internal.BaseGen<Property.Test<T>>, IFastProperty<T>, IFiniteGen
    {
        public long? Count(long limit) => Finite.Of(Input, limit);

        public void WriteChoices(long index, List<ulong> choices) =>
            ((IFiniteGen)Input.Advanced).WriteChoices(index, choices);

        public override Property.Test<T> Generate(GenSource source)
        {
            var input = Input.Advanced.Generate(source);
            return TestFactory.Create(input, () => Func(input), Presenter(input));
        }

        public T Run(GenSource source, bool trackTime, out long generatedAt, out bool failed, out Exception? exception)
        {
            var input = Input.Advanced.Generate(source);
            generatedAt = trackTime ? Environment.TickCount64 : 0;
            if (!source.ShouldRunTest())
            {
                failed = false;
                exception = null;
                return input;
            }

            try
            {
                failed = !Func(input);
                exception = null;
            }
            catch (PreconditionFailedException)
            {
                failed = false;
                exception = PreconditionFailedException.Instance;
            }
            catch (Exception ex)
            {
                failed = true;
                exception = ex;
            }

            return input;
        }

        public IReadOnlyList<object?> Present(object? input) => Presenter((T)input!);
    }
}
