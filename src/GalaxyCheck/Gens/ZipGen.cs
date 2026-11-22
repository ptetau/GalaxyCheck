using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using System.Collections.Generic;
using System.Linq;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        public static IGen<(T0, T1)> Zip<T0, T1>(IGen<T0> gen0, IGen<T1> gen1) =>
            from x0 in gen0
            from x1 in gen1
            select (x0, x1);

        public static IGen<(T0, T1, T2)> Zip<T0, T1, T2>(
            IGen<T0> gen0,
            IGen<T1> gen1,
            IGen<T2> gen2) =>
                from x0 in gen0
                from x1 in gen1
                from x2 in gen2
                select (x0, x1, x2);

        public static IGen<(T0, T1, T2, T3)> Zip<T0, T1, T2, T3>(
            IGen<T0> gen0,
            IGen<T1> gen1,
            IGen<T2> gen2,
            IGen<T3> gen3) =>
                from x0 in gen0
                from x1 in gen1
                from x2 in gen2
                from x3 in gen3
                select (x0, x1, x2, x3);

        public static IGen<IEnumerable<T>> Zip<T>(IEnumerable<IGen<T>> gens) =>
            new ZipManyGen<T>(gens.Select(gen => gen.Advanced).ToArray());
    }
}

namespace GalaxyCheck.Gens.Internal
{
    internal sealed record ZipManyGen<T>(IGenAdvanced<T>[] Gens) : BaseGen<IEnumerable<T>>
    {
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
