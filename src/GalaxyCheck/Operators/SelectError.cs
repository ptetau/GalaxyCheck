using GalaxyCheck.Gens.Internal;
using GalaxyCheck.Gens.Parameters;
using System;

namespace GalaxyCheck
{
    public static partial class Extensions
    {
        internal static IGen<T> SelectError<T>(this IGen<T> gen, Func<string, string> errorSelector) =>
            new SelectErrorGen<T>(gen, errorSelector);
    }
}

namespace GalaxyCheck.Gens.Internal
{
    internal sealed record SelectErrorGen<T>(IGen<T> Source, Func<string, string> ErrorSelector) : BaseGen<T>
    {
        public override T Generate(GenSource source)
        {
            try
            {
                return Source.Advanced.Generate(source);
            }
            catch (GenErrorSignal error)
            {
                throw new GenErrorSignal(ErrorSelector(error.Message));
            }
        }
    }
}
