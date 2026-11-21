namespace GalaxyCheck.Xunit.CodeAnalysis.Tests
{
    /// <summary>The reference assemblies of the target that the tests run on.</summary>
    internal static class TargetReference
    {
#if NET11_0_OR_GREATER
        public const string Name = "net11.0";
        public const string Version = "11.0.0";
#else
        public const string Name = "net10.0";
        public const string Version = "10.0.0";
#endif
    }
}
