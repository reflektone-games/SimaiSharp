using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace SimaiSharp.FileReading
{
    public sealed unsafe class MemoryMappedFileReader : IFileReader
    {
        private readonly MemoryMappedFile         _memoryMap;
        private readonly MemoryMappedViewAccessor _accessor;
        private readonly byte*                    _ptr;
        private readonly int                      _length;

        public MemoryMappedFileReader(string path)
        {
            _memoryMap = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            _accessor  = _memoryMap.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);
            _length = (int)_accessor.Capacity;

            // Skip UTF-8 BOM
            if (_accessor.Capacity > 3 && *_ptr == 0xEF && _ptr[1] == 0xBB && _ptr[2] == 0xBF)
            {
                _ptr   += 3;
                _length -= 3;
            }
        }

        public ReadOnlySpan<byte> GetSpan() => new(_ptr, _length);

        public void Dispose()
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor.Dispose();
            _memoryMap.Dispose();
        }
    }
}
