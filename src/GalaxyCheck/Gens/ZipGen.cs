using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using System.Collections.Generic;
using System.Linq;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        public static IGen<(T0, T1)> Zip<T0, T1>(IGen<T0> gen0, IGen<T1> gen1) =>
            new ZipGen2<T0, T1>(gen0, gen1);

        public static IGen<(T0, T1, T2)> Zip<T0, T1, T2>(
            IGen<T0> gen0,
            IGen<T1> gen1,
            IGen<T2> gen2) => new ZipGen3<T0, T1, T2>(gen0, gen1, gen2);

        public static IGen<(T0, T1, T2, T3)> Zip<T0, T1, T2, T3>(
            IGen<T0> gen0,
            IGen<T1> gen1,
            IGen<T2> gen2,
            IGen<T3> gen3) => new ZipGen4<T0, T1, T2, T3>(gen0, gen1, gen2, gen3);

        public static IGen<IEnumerable<T>> Zip<T>(IEnumerable<IGen<T>> gens) =>
            new ZipManyGen<T>(gens.Select(gen => gen.Advanced).ToArray());
    }
}

namespace GalaxyCheck.Gens.Internal
{
    internal sealed record ZipManyGen<T>(IGenAdvanced<T>[] Gens) : BaseGen<IEnumerable<T>>, GalaxyCheck.Properties.IFiniteGen
    {
        private GalaxyCheck.Properties.IFiniteGen?[] Parts =>
            System.Array.ConvertAll(Gens, g => g as GalaxyCheck.Properties.IFiniteGen);

        public long? Count(long limit) => GalaxyCheck.Properties.Finite.Product(Parts, limit);

        public void WriteChoices(long index, List<ulong> choices) =>
            GalaxyCheck.Properties.Finite.WriteProduct(Parts!, index, choices);

        public override IEnumerable<T> Generate(GenSource source)
        {
            var values = new T[Gens.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = Gens[i].Generate(source);
            }

            return values;
        }
    }
}

namespace GalaxyCheck.Gens.Internal
{
    using GalaxyCheck.Properties;

    internal sealed record ZipGen2<T0, T1>(IGen<T0> Gen0, IGen<T1> Gen1) : BaseGen<(T0, T1)>, IFiniteGen
    {
        public override (T0, T1) Generate(GenSource source)
        {
            var x0 = Gen0.Advanced.Generate(source);
            var x1 = Gen1.Advanced.Generate(source);
            return (x0, x1);
        }

        private IFiniteGen?[] Parts => new[] { Gen0.Advanced as IFiniteGen, Gen1.Advanced as IFiniteGen };

        public long? Count(long limit) => Finite.Product(Parts, limit);

        public void WriteChoices(long index, List<ulong> choices) => Finite.WriteProduct(Parts!, index, choices);
    }

    internal sealed record ZipGen3<T0, T1, T2>(IGen<T0> Gen0, IGen<T1> Gen1, IGen<T2> Gen2) : BaseGen<(T0, T1, T2)>, IFiniteGen
    {
        public override (T0, T1, T2) Generate(GenSource source)
        {
            var x0 = Gen0.Advanced.Generate(source);
            var x1 = Gen1.Advanced.Generate(source);
            var x2 = Gen2.Advanced.Generate(source);
            return (x0, x1, x2);
        }

        private IFiniteGen?[] Parts => new[] { Gen0.Advanced as IFiniteGen, Gen1.Advanced as IFiniteGen, Gen2.Advanced as IFiniteGen };

        public long? Count(long limit) => Finite.Product(Parts, limit);

        public void WriteChoices(long index, List<ulong> choices) => Finite.WriteProduct(Parts!, index, choices);
    }

    internal sealed record ZipGen4<T0, T1, T2, T3>(IGen<T0> Gen0, IGen<T1> Gen1, IGen<T2> Gen2, IGen<T3> Gen3) : BaseGen<(T0, T1, T2, T3)>, IFiniteGen
    {
        public override (T0, T1, T2, T3) Generate(GenSource source)
        {
            var x0 = Gen0.Advanced.Generate(source);
            var x1 = Gen1.Advanced.Generate(source);
            var x2 = Gen2.Advanced.Generate(source);
            var x3 = Gen3.Advanced.Generate(source);
            return (x0, x1, x2, x3);
        }

        private IFiniteGen?[] Parts => new[]
        {
            Gen0.Advanced as IFiniteGen, Gen1.Advanced as IFiniteGen, Gen2.Advanced as IFiniteGen, Gen3.Advanced as IFiniteGen
        };

        public long? Count(long limit) => Finite.Product(Parts, limit);

        public void WriteChoices(long index, List<ulong> choices) => Finite.WriteProduct(Parts!, index, choices);
    }
}
