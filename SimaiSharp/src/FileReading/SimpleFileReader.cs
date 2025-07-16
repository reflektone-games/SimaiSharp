using System;
using System.IO;

namespace SimaiSharp.FileReading
{
    public sealed class SimpleFileReader : IFileReader
    {
        private readonly byte[] _bytes;

        public SimpleFileReader(string path) => _bytes = File.ReadAllBytes(path);

        public ReadOnlySpan<byte> GetSpan() => _bytes;

        public void Dispose()
        {

        }
    }
}
