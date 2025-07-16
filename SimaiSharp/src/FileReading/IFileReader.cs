using System;

namespace SimaiSharp.FileReading
{
    public interface IFileReader : IDisposable
    {
        ReadOnlySpan<byte> GetSpan();
    }
}
