using GalaxyCheck.Gens.Internal;
using System.Collections.Generic;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        /// <summary>Generates key-value pairs from a key generator and a value generator.</summary>
        public static IGen<KeyValuePair<TKey, TValue>> KeyValuePair<TKey, TValue>(IGen<TKey> keyGen, IGen<TValue> valueGen)
        {
            var (k, v) = (keyGen.Advanced, valueGen.Advanced);
            return new FiniteFunctionGen<KeyValuePair<TKey, TValue>>(new IGen[] { keyGen, valueGen }, s =>
            {
                var key = k.Generate(s);
                return new KeyValuePair<TKey, TValue>(key, v.Generate(s));
            });
        }

        /// <summary>Generates value tuples whose elements come from the given generators, in order.</summary>
        public static IGen<System.ValueTuple<T1>> ValueTuple<T1>(IGen<T1> gen1)
        {
            var g1 = gen1.Advanced;
            return new FiniteFunctionGen<System.ValueTuple<T1>>(new IGen[] { gen1 }, s =>
            {
                var x1 = g1.Generate(s);
                return new System.ValueTuple<T1>(x1);
            });
        }

        /// <summary>Generates tuples whose elements come from the given generators, in order.</summary>
        public static IGen<System.Tuple<T1>> Tuple<T1>(IGen<T1> gen1)
        {
            var g1 = gen1.Advanced;
            return new FiniteFunctionGen<System.Tuple<T1>>(new IGen[] { gen1 }, s =>
            {
                var x1 = g1.Generate(s);
                return new System.Tuple<T1>(x1);
            });
        }

        /// <summary>Generates value tuples whose elements come from the given generators, in order.</summary>
        public static IGen<System.ValueTuple<T1, T2>> ValueTuple<T1, T2>(IGen<T1> gen1, IGen<T2> gen2)
        {
            var g1 = gen1.Advanced;
            var g2 = gen2.Advanced;
            return new FiniteFunctionGen<System.ValueTuple<T1, T2>>(new IGen[] { gen1, gen2 }, s =>
            {
                var x1 = g1.Generate(s);
                var x2 = g2.Generate(s);
                return new System.ValueTuple<T1, T2>(x1, x2);
            });
        }

        /// <summary>Generates tuples whose elements come from the given generators, in order.</summary>
        public static IGen<System.Tuple<T1, T2>> Tuple<T1, T2>(IGen<T1> gen1, IGen<T2> gen2)
        {
            var g1 = gen1.Advanced;
            var g2 = gen2.Advanced;
            return new FiniteFunctionGen<System.Tuple<T1, T2>>(new IGen[] { gen1, gen2 }, s =>
            {
                var x1 = g1.Generate(s);
                var x2 = g2.Generate(s);
                return new System.Tuple<T1, T2>(x1, x2);
            });
        }

        /// <summary>Generates value tuples whose elements come from the given generators, in order.</summary>
        public static IGen<System.ValueTuple<T1, T2, T3>> ValueTuple<T1, T2, T3>(IGen<T1> gen1, IGen<T2> gen2, IGen<T3> gen3)
        {
            var g1 = gen1.Advanced;
            var g2 = gen2.Advanced;
            var g3 = gen3.Advanced;
            return new FiniteFunctionGen<System.ValueTuple<T1, T2, T3>>(new IGen[] { gen1, gen2, gen3 }, s =>
            {
                var x1 = g1.Generate(s);
                var x2 = g2.Generate(s);
                var x3 = g3.Generate(s);
                return new System.ValueTuple<T1, T2, T3>(x1, x2, x3);
            });
        }

        /// <summary>Generates tuples whose elements come from the given generators, in order.</summary>
        public static IGen<System.Tuple<T1, T2, T3>> Tuple<T1, T2, T3>(IGen<T1> gen1, IGen<T2> gen2, IGen<T3> gen3)
        {
            var g1 = gen1.Advanced;
            var g2 = gen2.Advanced;
            var g3 = gen3.Advanced;
            return new FiniteFunctionGen<System.Tuple<T1, T2, T3>>(new IGen[] { gen1, gen2, gen3 }, s =>
            {
                var x1 = g1.Generate(s);
                var x2 = g2.Generate(s);
                var x3 = g3.Generate(s);
                return new System.Tuple<T1, T2, T3>(x1, x2, x3);
            });
        }

        /// <summary>Generates value tuples whose elements come from the given generators, in order.</summary>
        public static IGen<System.ValueTuple<T1, T2, T3, T4>> ValueTuple<T1, T2, T3, T4>(IGen<T1> gen1, IGen<T2> gen2, IGen<T3> gen3, IGen<T4> gen4)
        {
            var g1 = gen1.Advanced;
            var g2 = gen2.Advanced;
            var g3 = gen3.Advanced;
            var g4 = gen4.Advanced;
            return new FiniteFunctionGen<System.ValueTuple<T1, T2, T3, T4>>(new IGen[] { gen1, gen2, gen3, gen4 }, s =>
            {
                var x1 = g1.Generate(s);
                var x2 = g2.Generate(s);
                var x3 = g3.Generate(s);
                var x4 = g4.Generate(s);
                return new System.ValueTuple<T1, T2, T3, T4>(x1, x2, x3, x4);
            });
        }

        /// <summary>Generates tuples whose elements come from the given generators, in order.</summary>
        public static IGen<System.Tuple<T1, T2, T3, T4>> Tuple<T1, T2, T3, T4>(IGen<T1> gen1, IGen<T2> gen2, IGen<T3> gen3, IGen<T4> gen4)
        {
            var g1 = gen1.Advanced;
            var g2 = gen2.Advanced;
            var g3 = gen3.Advanced;
            var g4 = gen4.Advanced;
            return new FiniteFunctionGen<System.Tuple<T1, T2, T3, T4>>(new IGen[] { gen1, gen2, gen3, gen4 }, s =>
            {
                var x1 = g1.Generate(s);
                var x2 = g2.Generate(s);
                var x3 = g3.Generate(s);
                var x4 = g4.Generate(s);
                return new System.Tuple<T1, T2, T3, T4>(x1, x2, x3, x4);
            });
        }

        /// <summary>Generates value tuples whose elements come from the given generators, in order.</summary>
        public static IGen<System.ValueTuple<T1, T2, T3, T4, T5>> ValueTuple<T1, T2, T3, T4, T5>(IGen<T1> gen1, IGen<T2> gen2, IGen<T3> gen3, IGen<T4> gen4, IGen<T5> gen5)
        {
            var g1 = gen1.Advanced;
            var g2 = gen2.Advanced;
            var g3 = gen3.Advanced;
            var g4 = gen4.Advanced;
            var g5 = gen5.Advanced;
            return new FiniteFunctionGen<System.ValueTuple<T1, T2, T3, T4, T5>>(new IGen[] { gen1, gen2, gen3, gen4, gen5 }, s =>
            {
                var x1 = g1.Generate(s);
                var x2 = g2.Generate(s);
                var x3 = g3.Generate(s);
                var x4 = g4.Generate(s);
                var x5 = g5.Generate(s);
                return new System.ValueTuple<T1, T2, T3, T4, T5>(x1, x2, x3, x4, x5);
            });
        }

        /// <summary>Generates tuples whose elements come from the given generators, in order.</summary>
        public static IGen<System.Tuple<T1, T2, T3, T4, T5>> Tuple<T1, T2, T3, T4, T5>(IGen<T1> gen1, IGen<T2> gen2, IGen<T3> gen3, IGen<T4> gen4, IGen<T5> gen5)
        {
            var g1 = gen1.Advanced;
            var g2 = gen2.Advanced;
            var g3 = gen3.Advanced;
            var g4 = gen4.Advanced;
            var g5 = gen5.Advanced;
            return new FiniteFunctionGen<System.Tuple<T1, T2, T3, T4, T5>>(new IGen[] { gen1, gen2, gen3, gen4, gen5 }, s =>
            {
                var x1 = g1.Generate(s);
                var x2 = g2.Generate(s);
                var x3 = g3.Generate(s);
                var x4 = g4.Generate(s);
                var x5 = g5.Generate(s);
                return new System.Tuple<T1, T2, T3, T4, T5>(x1, x2, x3, x4, x5);
            });
        }

        /// <summary>Generates value tuples whose elements come from the given generators, in order.</summary>
        public static IGen<System.ValueTuple<T1, T2, T3, T4, T5, T6>> ValueTuple<T1, T2, T3, T4, T5, T6>(IGen<T1> gen1, IGen<T2> gen2, IGen<T3> gen3, IGen<T4> gen4, IGen<T5> gen5, IGen<T6> gen6)
        {
            var g1 = gen1.Advanced;
            var g2 = gen2.Advanced;
            var g3 = gen3.Advanced;
            var g4 = gen4.Advanced;
            var g5 = gen5.Advanced;
            var g6 = gen6.Advanced;
            return new FiniteFunctionGen<System.ValueTuple<T1, T2, T3, T4, T5, T6>>(new IGen[] { gen1, gen2, gen3, gen4, gen5, gen6 }, s =>
            {
                var x1 = g1.Generate(s);
                var x2 = g2.Generate(s);
                var x3 = g3.Generate(s);
                var x4 = g4.Generate(s);
                var x5 = g5.Generate(s);
                var x6 = g6.Generate(s);
                return new System.ValueTuple<T1, T2, T3, T4, T5, T6>(x1, x2, x3, x4, x5, x6);
            });
        }

        /// <summary>Generates tuples whose elements come from the given generators, in order.</summary>
        public static IGen<System.Tuple<T1, T2, T3, T4, T5, T6>> Tuple<T1, T2, T3, T4, T5, T6>(IGen<T1> gen1, IGen<T2> gen2, IGen<T3> gen3, IGen<T4> gen4, IGen<T5> gen5, IGen<T6> gen6)
        {
            var g1 = gen1.Advanced;
            var g2 = gen2.Advanced;
            var g3 = gen3.Advanced;
            var g4 = gen4.Advanced;
            var g5 = gen5.Advanced;
            var g6 = gen6.Advanced;
            return new FiniteFunctionGen<System.Tuple<T1, T2, T3, T4, T5, T6>>(new IGen[] { gen1, gen2, gen3, gen4, gen5, gen6 }, s =>
            {
                var x1 = g1.Generate(s);
                var x2 = g2.Generate(s);
                var x3 = g3.Generate(s);
                var x4 = g4.Generate(s);
                var x5 = g5.Generate(s);
                var x6 = g6.Generate(s);
                return new System.Tuple<T1, T2, T3, T4, T5, T6>(x1, x2, x3, x4, x5, x6);
            });
        }

        /// <summary>Generates value tuples whose elements come from the given generators, in order.</summary>
        public static IGen<System.ValueTuple<T1, T2, T3, T4, T5, T6, T7>> ValueTuple<T1, T2, T3, T4, T5, T6, T7>(IGen<T1> gen1, IGen<T2> gen2, IGen<T3> gen3, IGen<T4> gen4, IGen<T5> gen5, IGen<T6> gen6, IGen<T7> gen7)
        {
            var g1 = gen1.Advanced;
            var g2 = gen2.Advanced;
            var g3 = gen3.Advanced;
            var g4 = gen4.Advanced;
            var g5 = gen5.Advanced;
            var g6 = gen6.Advanced;
            var g7 = gen7.Advanced;
            return new FiniteFunctionGen<System.ValueTuple<T1, T2, T3, T4, T5, T6, T7>>(new IGen[] { gen1, gen2, gen3, gen4, gen5, gen6, gen7 }, s =>
            {
                var x1 = g1.Generate(s);
                var x2 = g2.Generate(s);
                var x3 = g3.Generate(s);
                var x4 = g4.Generate(s);
                var x5 = g5.Generate(s);
                var x6 = g6.Generate(s);
                var x7 = g7.Generate(s);
                return new System.ValueTuple<T1, T2, T3, T4, T5, T6, T7>(x1, x2, x3, x4, x5, x6, x7);
            });
        }

        /// <summary>Generates tuples whose elements come from the given generators, in order.</summary>
        public static IGen<System.Tuple<T1, T2, T3, T4, T5, T6, T7>> Tuple<T1, T2, T3, T4, T5, T6, T7>(IGen<T1> gen1, IGen<T2> gen2, IGen<T3> gen3, IGen<T4> gen4, IGen<T5> gen5, IGen<T6> gen6, IGen<T7> gen7)
        {
            var g1 = gen1.Advanced;
            var g2 = gen2.Advanced;
            var g3 = gen3.Advanced;
            var g4 = gen4.Advanced;
            var g5 = gen5.Advanced;
            var g6 = gen6.Advanced;
            var g7 = gen7.Advanced;
            return new FiniteFunctionGen<System.Tuple<T1, T2, T3, T4, T5, T6, T7>>(new IGen[] { gen1, gen2, gen3, gen4, gen5, gen6, gen7 }, s =>
            {
                var x1 = g1.Generate(s);
                var x2 = g2.Generate(s);
                var x3 = g3.Generate(s);
                var x4 = g4.Generate(s);
                var x5 = g5.Generate(s);
                var x6 = g6.Generate(s);
                var x7 = g7.Generate(s);
                return new System.Tuple<T1, T2, T3, T4, T5, T6, T7>(x1, x2, x3, x4, x5, x6, x7);
            });
        }
    }
}

namespace GalaxyCheck.Gens.Internal
{
    using GalaxyCheck.Gens.Parameters;
    using GalaxyCheck.Properties;

    /// <summary>
    /// A generator of a tuple-like value. It is finite when each part is finite, and then it counts like a product.
    /// </summary>
    [global::Verifiable.Contracts.Unverifiable]
    internal sealed record FiniteFunctionGen<T>(IGen[] Inputs, System.Func<GenSource, T> Func) : BaseGen<T>, IFiniteGen
    {
        public override T Generate(GenSource source) => Func(source);

        private IFiniteGen?[] Parts => System.Array.ConvertAll(Inputs, g => g.Advanced as IFiniteGen);

        public long? Count(long limit) => Finite.Product(Parts, limit);

        public void WriteChoices(long index, List<ulong> choices) => Finite.WriteProduct(Parts!, index, choices);
    }
}
