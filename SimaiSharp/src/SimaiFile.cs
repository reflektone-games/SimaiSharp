using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace SimaiSharp
{
    public sealed unsafe class SimaiFile : IDisposable
    {
        private          Dictionary<int, MemorySlice>? _entries;
        private readonly MemoryMappedFile              _memoryMap;
        private readonly MemoryMappedViewAccessor      _accessor;
        private readonly byte*                         _ptr;

        public SimaiFile(string path)
        {
            _memoryMap = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            _accessor  = _memoryMap.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);
        }

        /// <returns>A boolean indicating whether to decode the value</returns>
        public delegate bool OnKeyRead(string key);

        public delegate void OnValueRead(string key, string value);

        public Dictionary<int, MemorySlice> ParseFile()
        {
            var entries = new Dictionary<int, MemorySlice>();

            var fileLength = _accessor.Capacity;
            var bytes      = new Span<byte>(_ptr, (int)fileLength);

            var  keyHash    = 0;
            long keyStart   = 0;
            long valueStart = 0;
            var  readingKey = false;
            int  byteIndex;

            for (byteIndex = 0; byteIndex < fileLength; byteIndex++)
            {
                var currentByte = bytes[byteIndex];

                switch (currentByte)
                {
                    case (byte)'&': // New entry
                    {
                        if (keyStart < valueStart)
                            entries[keyHash] = new MemorySlice(valueStart, (int)(byteIndex - valueStart));

                        readingKey = true;
                        keyHash    = 0;
                        keyStart   = byteIndex + 1; // Skips the "&" character
                        break;
                    }
                    case (byte)'=' when readingKey:
                    {
                        var keyLength = byteIndex - (int)keyStart;
                        keyHash    = ComputeHash(bytes.Slice((int)keyStart, keyLength));
                        valueStart = byteIndex + 1;
                        readingKey = false;
                        break;
                    }
                    case 0:
                        goto FINALIZE;
                }
            }

        FINALIZE:
            if (keyStart < valueStart)
                entries[keyHash] = new MemorySlice(valueStart, (int)(byteIndex - valueStart));

            return entries;
        }

        public void Enumerate(OnKeyRead onKeyRead, OnValueRead? onValueRead)
        {
            var fileLength = _accessor.Capacity;
            var bytes      = new Span<byte>(_ptr, (int)fileLength);

            var  readingKey = false;
            long keyStart   = 0;
            long valueStart = 0;
            var  currentKey = string.Empty;
            var  sendValue  = false;
            int  byteIndex;

            for (byteIndex = 0; byteIndex < fileLength; byteIndex++)
            {
                var currentByte = bytes[byteIndex];

                switch (currentByte)
                {
                    case (byte)'&':
                    {
                        if (sendValue && !string.IsNullOrEmpty(currentKey))
                            onValueRead!.Invoke(
                                currentKey,
                                Encoding.UTF8.GetString(bytes.Slice((int)valueStart, (int)(byteIndex - valueStart))));

                        readingKey = true;
                        currentKey = string.Empty;
                        keyStart   = byteIndex + 1;
                        break;
                    }

                    case (byte)'=' when readingKey:
                    {
                        var keyLength = (int)(byteIndex - keyStart);
                        currentKey = Encoding.UTF8.GetString(bytes.Slice((int)keyStart, keyLength));
                        sendValue  = onKeyRead.Invoke(currentKey);

                        valueStart = byteIndex + 1;
                        readingKey = false;
                        break;
                    }

                    case 0: // Null terminator
                        goto FINALIZE;
                }
            }

        FINALIZE:
            if (sendValue && !string.IsNullOrEmpty(currentKey))
                onValueRead!.Invoke(
                    currentKey, Encoding.UTF8.GetString(bytes.Slice((int)valueStart, (int)(byteIndex - valueStart))));
        }

        public bool TryGetValueOnce(string key, out string value)
        {
            var targetKeyHash = ComputeHash(key);
            var fileLength    = _accessor.Capacity;
            var bytes         = new Span<byte>(_ptr, (int)fileLength);

            var  keyHash    = 0;
            long keyStart   = 0;
            long valueStart = 0;
            var  readingKey = false;
            int  byteIndex;

            for (byteIndex = 0; byteIndex < fileLength; byteIndex++)
            {
                var currentByte = bytes[byteIndex];

                switch (currentByte)
                {
                    case (byte)'&': // New entry
                    {
                        if (keyHash == targetKeyHash)
                        {
                            value = GetString((int)valueStart, (int)(byteIndex - valueStart));
                            return true;
                        }

                        readingKey = true;
                        keyHash    = 0;
                        keyStart   = byteIndex + 1; // Skips the "&" character
                        break;
                    }
                    case (byte)'=' when readingKey:
                    {
                        var keyLength = byteIndex - (int)keyStart;
                        keyHash    = ComputeHash(bytes.Slice((int)keyStart, keyLength));
                        valueStart = byteIndex + 1;
                        readingKey = false;
                        break;
                    }
                    case 0:
                        goto FINALIZE;
                }
            }

        FINALIZE:
            if (keyHash == targetKeyHash)
            {
                value = GetString((int)valueStart, (int)(byteIndex - valueStart));
                return true;
            }

            value = string.Empty;
            return false;
        }

        public bool TryGetValueSpan(string key, out Span<byte> result)
        {
            _entries ??= ParseFile();

            if (_entries.TryGetValue(ComputeHash(key.AsSpan()), out var entry))
            {
                result = new Span<byte>(_ptr + entry.offset, entry.length);
                return true;
            }

            result = null;
            return false;
        }

        public bool TryGetValue(string key, out string value)
        {
            _entries ??= ParseFile();

            if (_entries.TryGetValue(ComputeHash(key.AsSpan()), out var entry))
            {
                value = GetString(entry);
                return true;
            }

            value = string.Empty;
            return false;
        }

        public MemorySlice this[string key]
        {
            get
            {
                _entries ??= ParseFile();

                if (_entries.TryGetValue(ComputeHash(key), out var value))
                    return value;

                throw new KeyNotFoundException($"Key '{key}' is not present in the SimaiFile.");
            }
        }

        public string GetString(MemorySlice slice) =>
            GetString((int)slice.offset, slice.length);

        /// <summary>
        /// https://stackoverflow.com/questions/7956167/how-can-i-quickly-read-bytes-from-a-memory-mapped-file-in-net
        /// </summary>
        private string GetString(int offset, int length)
        {
            var result = Encoding.UTF8.GetString(_ptr + offset, length);
            return result;
        }

        /// <summary>
        /// https://stackoverflow.com/questions/16340/how-do-i-generate-a-hashcode-from-a-byte-array-in-c
        /// </summary>
        public static int ComputeHash(Span<byte> data)
        {
            unchecked
            {
                const int p    = 16777619;
                var       hash = (int)2166136261;

                for (var i = 0; i < data.Length; i++)
                    hash = (hash ^ data[i]) * p;

                return hash;
            }
        }

        public static int ComputeHash(ReadOnlySpan<char> data)
        {
            unchecked
            {
                const int p    = 16777619;
                var       hash = (int)2166136261;

                for (var i = 0; i < data.Length; i++)
                {
                    var c = data[i];
                    for (; c > 0; c >>= 8)
                        hash = (hash ^ (c & 0xFF)) * p;
                }

                return hash;
            }
        }

        public struct MemorySlice
        {
            public readonly long offset;
            public readonly int  length;

            public MemorySlice(long offset, int length)
            {
                this.offset = offset;
                this.length = length;
            }
        }

        public void Dispose()
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor.Dispose();
            _memoryMap.Dispose();
        }
    }
}
