using GalaxyCheck;
using System;
using System.Linq;

namespace Tests.V2
{
    public static class DomainGen
    {
        public static IGen<int> Seed() => GalaxyCheck.Gen.Int32().WithBias(GalaxyCheck.Gen.Bias.None).NoShrink();

        public static IGen<int> Size(bool allowChaos = true) => allowChaos
            ? GalaxyCheck.Gen.Int32().Between(0, 100)
            : GalaxyCheck.Gen.Int32().Between(0, 50);

        public static IGen<int> Iterations(bool allowZero = true) => GalaxyCheck.Gen.Int32().Between(allowZero ? 0 : 1, 200);

        public static IGen<GalaxyCheck.IGen<object>> Gen() =>
            GalaxyCheck.Gen.Choose<GalaxyCheck.IGen>(
                GalaxyCheck.Gen.Constant(GalaxyCheck.Gen.Int32().Between(-1000, 1000)),
                GalaxyCheck.Gen.Constant(GalaxyCheck.Gen.Int32().Between(0, 1).WithBias(GalaxyCheck.Gen.Bias.None))
            ).Select(gen => gen.Cast<object>());

        public static IGen<GalaxyCheck.Gen.Bias> Bias() =>
            GalaxyCheck.Gen.Element(new[] { GalaxyCheck.Gen.Bias.None, GalaxyCheck.Gen.Bias.WithSize });

        public static IGen<(T, T)> Two<T>(this IGen<T> gen) => GalaxyCheck.Gen.Zip(gen, gen);

        public static IGen<(T, T, T)> Three<T>(this IGen<T> gen) => GalaxyCheck.Gen.Zip(gen, gen, gen);

        public static IGen<(T, T, T, T)> Four<T>(this IGen<T> gen) => GalaxyCheck.Gen.Zip(gen, gen, gen, gen    );

        public static IGen<object> Any() => GalaxyCheck.Gen.Choose(
            GalaxyCheck.Gen.Int32().Cast<object>(),
            GalaxyCheck.Gen.Boolean().Cast<object>());

        public static GalaxyCheck.Gens.IListGen<object> AnyList() => Any().ListOf();

        public static IGen<Action> Action() =>
            GalaxyCheck.Gen.Boolean().Select<bool, Action>(b => () => IfTrueThenThrow(b));

        public static IGen<Action<T0>> Action<T0>() =>
            GalaxyCheck.Gen.Boolean().Select<bool, Action<T0>>(b => (_) => IfTrueThenThrow(b));

        public static IGen<Action<T0, T1>> Action<T0, T1>() =>
            GalaxyCheck.Gen.Boolean().Select<bool, Action<T0, T1>>(b => (_, __) => IfTrueThenThrow(b));

        public static IGen<Action<T0, T1, T2>> Action<T0, T1, T2>() =>
            GalaxyCheck.Gen.Boolean().Select<bool, Action<T0, T1, T2>>(b => (_, __, ___) => IfTrueThenThrow(b));

        public static IGen<Action<T0, T1, T2, T3>> Action<T0, T1, T2, T3>() =>
            GalaxyCheck.Gen.Boolean().Select<bool, Action<T0, T1, T2, T3>>(b => (_, __, ___, ____) => IfTrueThenThrow(b));

        public static IGen<Func<T, bool>> Predicate<T>() => GalaxyCheck.Gen.Function<T, bool>(GalaxyCheck.Gen.Boolean());

        private static void IfTrueThenThrow(bool b)
        {
            if (b == true)
            {
                throw new Exception("Action threw");
            }
        }
    }
}
