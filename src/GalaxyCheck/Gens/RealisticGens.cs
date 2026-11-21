using System.Collections.Generic;

namespace GalaxyCheck
{
    public static partial class Gen
    {
        /// <summary>Generators of realistic English data.</summary>
        public static class Realistic
        {
            public static IReadOnlyList<string> FirstNames => throw new System.NotImplementedException("not implemented");

            public static IReadOnlyList<string> LastNames => throw new System.NotImplementedException("not implemented");

            public static IGen<string> FirstName() => throw new System.NotImplementedException("not implemented");

            public static IGen<string> LastName() => throw new System.NotImplementedException("not implemented");

            public static IGen<string> FullName() => throw new System.NotImplementedException("not implemented");

            public static IGen<string> StreetAddress() => throw new System.NotImplementedException("not implemented");

            public static IGen<string> City() => throw new System.NotImplementedException("not implemented");

            public static IGen<string> Country() => throw new System.NotImplementedException("not implemented");

            public static IGen<string> PhoneNumber() => throw new System.NotImplementedException("not implemented");

            public static IGen<string> CompanyName() => throw new System.NotImplementedException("not implemented");

        }
    }
}
