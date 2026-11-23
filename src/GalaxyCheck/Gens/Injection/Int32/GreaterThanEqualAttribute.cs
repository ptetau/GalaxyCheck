using System;

namespace GalaxyCheck.Gens.Injection.Int32
{
    /// <summary>Generates an int parameter at or above a minimum.</summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class GreaterThanEqualAttribute : GenAttribute
    {
        /// <summary>The inclusive minimum.</summary>
        public int Min { get; }

        /// <summary>Creates the attribute with an inclusive minimum.</summary>
        public GreaterThanEqualAttribute(int min)
        {
            Min = min;
        }

        /// <summary>A generator of ints at or above the minimum.</summary>
        public override IGen Value => GalaxyCheck.Gen.Int32().GreaterThanEqual(Min);
    }
}
