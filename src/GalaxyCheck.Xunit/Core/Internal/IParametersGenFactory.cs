using GalaxyCheck.Gens;
using System.Collections.Generic;
using System.Reflection;

namespace GalaxyCheck.Internal
{
#if GALAXYCHECK_XUNIT2
    public
#else
    internal
#endif
    interface IParametersGenFactory
    {
        IGen<object[]> CreateParametersGen(
            MethodInfo methodInfo,
            IGenFactory? genFactory,
            IReadOnlyDictionary<int, IGen> customGens);
    }
}
