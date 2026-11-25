using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Verifiable.Contracts;

namespace GalaxyCheck.Runners.Replaying
{
    /// <summary>
    /// A replay holds the size and the choices of one test case. Replaying the choices gives the same value.
    /// </summary>
    [Unverifiable]
    internal record Replay(int Seed, int Size, ulong[] Choices);

    [TypeAlias<int>("Replay choice length must be non-negative and at most 1,048,576.")]
    internal readonly partial record struct ReplayChoiceCount
    {
        public static bool IsValid(int value) => value >= 0 && value <= 1_048_576;
    }

    [Verifiable]
    internal readonly record struct ReplayHeader(int Seed, int Size, int Length)
    {
        [Invariant("Replay header size is 0..100 and choice length is 0..1,048,576.")]
        public bool IsValid() => Size >= 0 && Size <= 100 && Length >= 0 && Length <= 1_048_576;
    }

    internal enum ReplayDecodeErrorKind : byte
    {
        InvalidVersion,
        HeaderOutOfRange,
        TruncatedVarint,
    }

    [Verifiable]
    internal static class ReplayProtocol
    {
        public const byte Version = 2;
        public const int MaxChoices = 1_048_576;

        public static Result<ReplayHeader, ReplayDecodeErrorKind> ValidateHeader(int version, int seed, long size, long length)
        {
            Spec.Ensures<Result<ReplayHeader, ReplayDecodeErrorKind>>(
                r => !r.IsSuccess || (r.Value.Size >= 0 && r.Value.Size <= 100 && r.Value.Length >= 0 && r.Value.Length <= MaxChoices),
                "Validated replay headers always have size in 0..100 and length in 0..MaxChoices.");

            if (version != Version)
            {
                return Result<ReplayHeader, ReplayDecodeErrorKind>.Fail(ReplayDecodeErrorKind.InvalidVersion);
            }

            if (size < 0 || size > 100 || length < 0 || length > MaxChoices)
            {
                return Result<ReplayHeader, ReplayDecodeErrorKind>.Fail(ReplayDecodeErrorKind.HeaderOutOfRange);
            }

            return Result<ReplayHeader, ReplayDecodeErrorKind>.Ok(new ReplayHeader(seed, (int)size, (int)length));
        }

        public static int RemainingVarintBytesBounds(int position, int totalLength)
        {
            Spec.Requires(position >= 0 && totalLength >= position, "Position is within byte buffer bounds.");
            Spec.Ensures<int>(r => r == totalLength - position && r >= 0, "Remaining byte count is non-negative.");
            return totalLength - position;
        }
    }

    [Unverifiable]
    internal static class ReplayEncoding
    {
        private const byte Version = ReplayProtocol.Version;
        private const int MaxChoices = ReplayProtocol.MaxChoices;

        public static string Encode(int seed, int size, ulong[] choices, int length)
        {
            var bytes = new List<byte>(16 + length * 2) { Version };
            WriteVarint(bytes, (uint)seed);
            WriteVarint(bytes, (ulong)size);
            WriteVarint(bytes, (ulong)length);
            for (var i = 0; i < length; i++)
            {
                WriteVarint(bytes, choices[i]);
            }

            return Convert.ToBase64String(bytes.ToArray());
        }

        public static Replay Decode(string str)
        {
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(str);
            }
            catch (FormatException)
            {
                throw Invalid(str);
            }

            var position = 0;
            if (bytes.Length == 0) throw Invalid(str);
            var version = bytes[position++];

            var seed = (int)(uint)ReadVarint(bytes, ref position, str);
            var size = ReadVarint(bytes, ref position, str);
            var length = ReadVarint(bytes, ref position, str);

            var header = (size > int.MaxValue || length > int.MaxValue)
                ? Result<ReplayHeader, ReplayDecodeErrorKind>.Fail(ReplayDecodeErrorKind.HeaderOutOfRange)
                : ReplayProtocol.ValidateHeader(version, seed, (long)size, (long)length);
            if (!header.IsSuccess) throw Invalid(str);

            var choices = new ulong[header.Value.Length];
            for (var i = 0; i < choices.Length; i++)
            {
                choices[i] = ReadVarint(bytes, ref position, str);
            }

            if (position != bytes.Length) throw Invalid(str);

            return new Replay(header.Value.Seed, header.Value.Size, choices);
        }

        private static void WriteVarint(List<byte> bytes, ulong value)
        {
            while (value >= 0x80)
            {
                bytes.Add((byte)(value | 0x80));
                value >>= 7;
            }

            bytes.Add((byte)value);
        }

        private static ulong ReadVarint(byte[] bytes, ref int position, string str)
        {
            ulong value = 0;
            for (var shift = 0; shift < 64; shift += 7)
            {
                if (position >= bytes.Length) throw Invalid(str);
                var b = bytes[position++];
                value |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) return value;
            }

            throw Invalid(str);
        }

        private static Exception Invalid(string str) =>
            new FormatException($"The replay '{str}' is not valid. It may come from another version of GalaxyCheck.");
    }
}
