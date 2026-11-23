using System;
using System.Linq;
using Xunit.Sdk;

namespace GalaxyCheck.Internal.Infrastructure.Xunit
{
    internal static class ConstructorArguments
    {
        /// <summary>
        /// Replaces each deferred output helper with the helper itself. xunit 2.9 passes the output helper of a test
        /// class as a <c>Func&lt;TestOutputHelper&gt;</c>, and the constructor of the test class needs the helper.
        /// </summary>
        public static object[] Resolve(object[] constructorArguments) => constructorArguments
            .Select(argument => argument is Func<TestOutputHelper> createHelper ? createHelper() : argument)
            .ToArray();
    }
}
