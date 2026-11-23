using GalaxyCheck.Gens;
using System.Collections.Generic;
using System.Reflection;

namespace GalaxyCheck.Internal
{
    /// <summary>Creates the generator of the parameters of a test method.</summary>
#if GALAXYCHECK_XUNIT2
    public
#else
    internal
#endif
    interface IParametersGenFactory
    {
        /// <summary>Returns a generator of the arguments of the method.</summary>
        IGen<object[]> CreateParametersGen(
            MethodInfo methodInfo,
            IGenFactory? genFactory,
            IReadOnlyDictionary<int, IGen> customGens);
    }
}
