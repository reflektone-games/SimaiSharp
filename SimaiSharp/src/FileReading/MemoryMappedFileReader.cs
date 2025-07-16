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

        public MemoryMappedFileReader(string path)
        {
            _memoryMap = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            _accessor  = _memoryMap.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);
        }

        public ReadOnlySpan<byte> GetSpan()
        {
            var capacity = _accessor.Capacity;
            return new ReadOnlySpan<byte>(_ptr, (int)capacity);
        }

        public void Dispose()
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor.Dispose();
            _memoryMap.Dispose();
        }
    }
}
