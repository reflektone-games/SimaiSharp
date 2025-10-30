using System;
using System.IO;

namespace SimaiSharp.FileReading
{
    public interface ISimaiFileReader : IDisposable
    {
        ReadOnlySpan<byte> GetSpan();

        public static ISimaiFileReader Create(Stream stream) => BufferedSimaiFileReader.Create(stream);
        public static ISimaiFileReader Create(string path) => MemoryMappedSimaiFileReader.Create(path);
    }
}
