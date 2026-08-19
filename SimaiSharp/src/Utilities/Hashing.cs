using System;

namespace SimaiSharp.Utilities
{
    // https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    public struct ChartHasher()
    {
        private const ulong FnvOffsetBasis = 14695981039346656037;
        private const ulong FnvPrime       = 1099511628211;

        private ulong _hash = FnvOffsetBasis;

        public void Append(byte b)
        {
            unchecked
            {
                _hash ^= b;
                _hash *= FnvPrime;
            }
        }

        public ulong GetHash() => _hash;

        public void Clear() => _hash = FnvOffsetBasis;
    }

    public static class Hashing
    {
        /// <summary>
        /// https://stackoverflow.com/questions/16340/how-do-i-generate-a-hashcode-from-a-byte-array-in-c
        /// </summary>
        public static int ComputeHash(ReadOnlySpan<byte> bytes)
        {
            const int p = 16777619;
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var @byte in bytes)
                    hash = (hash ^ @byte) * p;
                return hash;
            }
        }

        public static int ComputeHash(ReadOnlySpan<char> chars)
        {
            const int p = 16777619;
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var @char in chars)
                    for (var c = @char; c > 0; c >>= 8)
                        hash = (hash ^ (c & 0xFF)) * p;
                return hash;
            }
        }
    }
}
