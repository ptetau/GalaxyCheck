using System;

namespace GalaxyCheck.Gens.Injection.Int32
{
    /// <summary>Generates an int parameter at or below a maximum.</summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    [global::Verifiable.Contracts.Unverifiable]
    public class LessThanEqualAttribute : GenAttribute
    {
        /// <summary>The inclusive maximum.</summary>
        public int Max { get; }


        /// <summary>Creates the attribute with an inclusive maximum.</summary>
        public LessThanEqualAttribute(int max)
        {
            Max = max;
        }

        /// <summary>A generator of ints at or below the maximum.</summary>
        public override IGen Value => GalaxyCheck.Gen.Int32().LessThanEqual(Max);
    }
}
