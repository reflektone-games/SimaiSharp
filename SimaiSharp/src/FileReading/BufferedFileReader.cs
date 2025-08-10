using System;
using System.IO;

namespace SimaiSharp.FileReading
{
    public sealed class BufferedFileReader : IFileReader
    {
        private readonly byte[] _bytes;

        public unsafe BufferedFileReader(string path)
        {
            using var  file   = File.OpenRead(path);
            Span<byte> tester = stackalloc byte[3];
            if (file.CanSeek && file.Read(tester) == 3 && tester[0] == 0xEF && tester[1] == 0xBB && tester[2] == 0xBF)
            {
                file.Position = 3;
                _bytes        = new byte[file.Length - 3];
            }
            else
            {
                file.Position = 0;
                _bytes        = new byte[file.Length];
            }

            var offset = 0;
            while (true)
            {
                var bytesRead = file.Read(_bytes, offset, _bytes.Length - offset);
                if (bytesRead == 0)
                    break;
                offset += bytesRead;
            }
        }

        public ReadOnlySpan<byte> GetSpan() => _bytes.AsSpan();

        public void Dispose()
        {
        }
    }
}
