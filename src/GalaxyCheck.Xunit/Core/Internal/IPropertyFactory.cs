using GalaxyCheck.Gens;
using System.Collections.Generic;
using System.Reflection;

namespace GalaxyCheck.Internal
{
    /// <summary>Creates the property of a test method.</summary>
#if GALAXYCHECK_XUNIT2
    public
#else
    internal
#endif
    interface IPropertyFactory
    {
        /// <summary>Returns the property that runs the method on generated arguments.</summary>
        AsyncProperty CreateProperty(
            MethodInfo methodInfo,
            object? target,
            IGenFactory? genFactory,
            IReadOnlyDictionary<int, IGen> customGens,
            object?[]? controlData);
    }
}
