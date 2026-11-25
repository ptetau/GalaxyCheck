namespace GalaxyCheck
{
    public static partial class Gen
    {
        /// <summary>
        /// Generates GUIDs. A GUID does not shrink.
        /// </summary>
        /// <returns>The new generator.</returns>
        public static IGen<System.Guid> Guid() => new Gens.GuidGen();
    }
}

namespace GalaxyCheck.Gens
{
    using GalaxyCheck.Gens.Internal;
    using GalaxyCheck.Gens.Parameters;
    using System;

    [global::Verifiable.Contracts.Unverifiable]
    internal sealed record GuidGen : BaseGen<Guid>
    {
        public override Guid Generate(GenSource source)
        {
            var high = source.DrawUniform(ulong.MaxValue);
            source.FreezeLast();
            var low = source.DrawUniform(ulong.MaxValue);
            source.FreezeLast();

            Span<byte> bytes = stackalloc byte[16];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(bytes, high);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(bytes.Slice(8), low);
            return new Guid(bytes);
        }
    }
}
