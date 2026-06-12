using System;
using System.IO;

namespace SimaiSharp.FileReading
{
    public interface IFileReader : IDisposable
    {
        ReadOnlySpan<byte> GetSpan();

        public static IFileReader Create(Stream stream) => BufferedFileReader.Create(stream);
        public static IFileReader Create(string path) => MemoryMappedFileReader.Create(path);
    }
}
