using Microsoft.CodeAnalysis;

namespace GalaxyCheck.SourceGeneration
{
    /// <summary>Stub. It will produce the code of Gen.Create for each type that a project asks for.</summary>
    [Generator]
    public sealed class GenCreateGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // not implemented
        }
    }
}
