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
    interface IPropertyFactory
    {
        AsyncProperty CreateProperty(
            MethodInfo methodInfo,
            object? target,
            IGenFactory? genFactory,
            IReadOnlyDictionary<int, IGen> customGens,
            object?[]? controlData);
    }
}
