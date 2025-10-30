using System;
using System.Buffers;
using System.IO;

namespace SimaiSharp.FileReading
{
    internal sealed class BufferedSimaiFileReader : ISimaiFileReader
    {
        private IMemoryOwner<byte>? _bytes;

        internal static BufferedSimaiFileReader Create(string path)
        {
            using var file = File.OpenRead(path);
            return Create(file);
        }

        internal static unsafe BufferedSimaiFileReader Create(Stream stream)
        {
            var        result = new BufferedSimaiFileReader();
            Span<byte> tester = stackalloc byte[3];
            if (stream.CanSeek && stream.Read(tester) == 3 && tester[0] == 0xEF && tester[1] == 0xBB && tester[2] == 0xBF)
            {
                stream.Position = 3;
                result._bytes   = MemoryPool<byte>.Shared.Rent((int)(stream.Length - 3));
            }
            else
            {
                stream.Position = 0;
                result._bytes   = MemoryPool<byte>.Shared.Rent((int)stream.Length);
            }

            var offset = 0;
            while (true)
            {
                var bytesRead = stream.Read(result._bytes.Memory[offset..].Span);
                if (bytesRead == 0)
                    break;
                offset += bytesRead;
            }

            return result;
        }

        public ReadOnlySpan<byte> GetSpan() => _bytes != null ? _bytes.Memory.Span : Span<byte>.Empty;

        public void Dispose() => _bytes?.Dispose();
    }
}
