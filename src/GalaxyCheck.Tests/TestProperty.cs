using GalaxyCheck.Properties;
using System;

namespace Tests.V2
{
    /// <summary>
    /// Builds the tests of the properties that check the library with the library.
    /// </summary>
    public static class TestProperty
    {
        public static GalaxyCheck.Property.Test ForThese(Action action) => ForThese(action.AsTrueFunc());

        public static GalaxyCheck.Property.Test ForThese(Func<bool> func) =>
            TestFactory.Create(Array.Empty<object>(), func, Array.Empty<object?>());
    }
}
