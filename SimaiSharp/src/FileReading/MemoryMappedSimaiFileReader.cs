using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace SimaiSharp.FileReading
{
    internal sealed unsafe class MemoryMappedSimaiFileReader : ISimaiFileReader
    {
        private MemoryMappedFile?         _memoryMap;
        private MemoryMappedViewAccessor? _accessor;
        private byte*                     _ptr;
        private int                       _length;

        public static MemoryMappedSimaiFileReader Create(string path)
        {
            var result = new MemoryMappedSimaiFileReader();
            result._memoryMap = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            result._accessor  = result._memoryMap.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            result._accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref result._ptr);
            result._length = (int)result._accessor.Capacity;

            // Skip UTF-8 BOM
            if (result._accessor.Capacity > 3     &&
                *result._ptr              == 0xEF &&
                result._ptr[1]            == 0xBB &&
                result._ptr[2]            == 0xBF)
            {
                result._ptr    += 3;
                result._length -= 3;
            }

            return result;
        }

        public ReadOnlySpan<byte> GetSpan() => new(_ptr, _length);

        public void Dispose()
        {
            _accessor?.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor?.Dispose();
            _memoryMap?.Dispose();
        }
    }
}
