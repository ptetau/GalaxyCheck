using System;
using System.Collections.Generic;

namespace GalaxyCheck.Runners.Replaying
{
    /// <summary>
    /// A replay holds the size and the choices of one test case. Replaying the choices gives the same value.
    /// </summary>
    internal record Replay(int Seed, int Size, ulong[] Choices);

    internal static class ReplayEncoding
    {
        private const byte Version = 2;
        private const int MaxChoices = 1 << 20;

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
            if (bytes.Length == 0 || bytes[position++] != Version) throw Invalid(str);

            var seed = (int)(uint)ReadVarint(bytes, ref position, str);
            var size = ReadVarint(bytes, ref position, str);
            var length = ReadVarint(bytes, ref position, str);

            if (size > 100 || length > MaxChoices) throw Invalid(str);

            var choices = new ulong[length];
            for (var i = 0; i < choices.Length; i++)
            {
                choices[i] = ReadVarint(bytes, ref position, str);
            }

            if (position != bytes.Length) throw Invalid(str);

            return new Replay(seed, (int)size, choices);
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
