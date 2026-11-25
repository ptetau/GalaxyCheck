using System;

namespace GalaxyCheck.Gens.Injection.Int32
{
    /// <summary>Generates an int parameter between two bounds, both inclusive.</summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    [global::Verifiable.Contracts.Unverifiable]
    public class BetweenAttribute : GenAttribute
    {
        /// <summary>The first bound.</summary>
        public int X { get; }

        /// <summary>The second bound.</summary>
        public int Y { get; }


        /// <summary>Creates the attribute with two inclusive bounds.</summary>
        public BetweenAttribute(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>A generator of ints between the bounds.</summary>
        public override IGen Value => GalaxyCheck.Gen.Int32().Between(X, Y);
    }
}
