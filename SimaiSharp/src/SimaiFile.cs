using System;
using System.Collections.Generic;
using System.Text;
using SimaiSharp.FileReading;

namespace SimaiSharp
{
    public sealed class SimaiFile : IDisposable
    {
        private          Dictionary<int, MemorySlice>? _entries;
        private readonly IFileReader                   _fileReader;

        public SimaiFile(IFileReader fileReader) => _fileReader = fileReader;

        public static SimaiFile FromMemoryMappedFile(string path) =>
            new(new MemoryMappedFileReader(path));

        public static SimaiFile FromFile(string path) =>
            new(new SimpleFileReader(path));

        /// <returns>A boolean indicating whether to decode the value</returns>
        public delegate void OnEntryRead(string key, ReadOnlySpan<byte> value);

        public Dictionary<int, MemorySlice> ParseFile()
        {
            var entries = new Dictionary<int, MemorySlice>();
            var bytes   = _fileReader.GetSpan();

            var  keyHash    = 0;
            var  keyStart   = 0;
            var  valueStart = 0;
            var  readingKey = false;
            byte lastByte   = 0;
            int  byteIndex;

            for (byteIndex = 0; byteIndex < bytes.Length; byteIndex++)
            {
                var currentByte = bytes[byteIndex];

                switch (currentByte)
                {
                    case (byte)'&' when lastByte is (byte)'\0' or (byte)'\n' or (byte)'\r':
                    {
                        if (keyStart < valueStart)
                            entries[keyHash] = new MemorySlice(valueStart, byteIndex - valueStart);

                        readingKey = true;
                        keyHash    = 0;
                        keyStart   = byteIndex + 1; // Skips the "&" character
                        break;
                    }
                    case (byte)'=' when readingKey:
                    {
                        readingKey = false;
                        var keyLength = byteIndex - keyStart;
                        keyHash    = ComputeHash(bytes.Slice(keyStart, keyLength));
                        valueStart = byteIndex + 1;
                        break;
                    }
                    case 0:
                        goto FINALIZE;
                }

                lastByte = currentByte;
            }

        FINALIZE:
            if (keyStart < valueStart)
                entries[keyHash] = new MemorySlice(valueStart, byteIndex - valueStart);

            return entries;
        }

        public void Enumerate(OnEntryRead onEntryRead)
        {
            var bytes = _fileReader.GetSpan();

            var  readingKey = false;
            long keyStart   = 0;
            long valueStart = 0;
            var  currentKey = string.Empty;
            byte lastByte   = 0;
            int  byteIndex;

            for (byteIndex = 0; byteIndex < bytes.Length; byteIndex++)
            {
                var currentByte = bytes[byteIndex];

                switch (currentByte)
                {
                    case (byte)'&' when lastByte is (byte)'\0' or (byte)'\n' or (byte)'\r':
                    {
                        if (keyStart < valueStart)
                            onEntryRead.Invoke(currentKey, bytes.Slice((int)valueStart, (int)(byteIndex - valueStart)));

                        readingKey = true;
                        currentKey = string.Empty;
                        keyStart   = byteIndex + 1;
                        break;
                    }

                    case (byte)'=' when readingKey:
                    {
                        readingKey = false;
                        var keyLength = (int)(byteIndex - keyStart);
                        currentKey = Encoding.UTF8.GetString(bytes.Slice((int)keyStart, keyLength));
                        valueStart = byteIndex + 1;
                        break;
                    }
                    case 0:
                        goto FINALIZE;
                }

                lastByte = currentByte;
            }

        FINALIZE:
            if (keyStart < valueStart)
                onEntryRead.Invoke(currentKey, bytes.Slice((int)valueStart, (int)(byteIndex - valueStart)));
        }

        public bool TryGetValueOnce(string key, out string value)
        {
            var targetKeyHash = ComputeHash(key);
            var bytes         = _fileReader.GetSpan();

            var  keyHash    = 0;
            long keyStart   = 0;
            long valueStart = 0;
            var  readingKey = false;
            byte lastByte   = 0;
            int  byteIndex;

            for (byteIndex = 0; byteIndex < bytes.Length; byteIndex++)
            {
                var currentByte = bytes[byteIndex];

                switch (currentByte)
                {
                    case (byte)'&' when lastByte is (byte)'\0' or (byte)'\n' or (byte)'\r':
                    {
                        if (keyHash == targetKeyHash)
                        {
                            value = Encoding.UTF8.GetString(bytes.Slice((int)valueStart, (int)(byteIndex - valueStart)));
                            return true;
                        }

                        readingKey = true;
                        keyHash    = 0;
                        keyStart   = byteIndex + 1;
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

                lastByte = currentByte;
            }

        FINALIZE:
            if (keyHash == targetKeyHash)
            {
                value = Encoding.UTF8.GetString(bytes.Slice((int)valueStart, (int)(byteIndex - valueStart)));
                return true;
            }

            value = string.Empty;
            return false;
        }

        public bool TryGetValueSpan(string key, out ReadOnlySpan<byte> result)
        {
            _entries ??= ParseFile();

            if (_entries.TryGetValue(ComputeHash(key.AsSpan()), out var entry))
            {
                result = _fileReader.GetSpan().Slice(entry.offset, entry.length);
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
                value = GetString(_fileReader.GetSpan(), entry);
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

        /// <summary>
        /// https://stackoverflow.com/questions/7956167/how-can-i-quickly-read-bytes-from-a-memory-mapped-file-in-net
        /// </summary>
        public string GetString(ReadOnlySpan<byte> bytes, MemorySlice slice) =>
            Encoding.UTF8.GetString(bytes.Slice(slice.offset, slice.length));

        /// <summary>
        /// https://stackoverflow.com/questions/16340/how-do-i-generate-a-hashcode-from-a-byte-array-in-c
        /// </summary>
        public static int ComputeHash(ReadOnlySpan<byte> data)
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
            public readonly int offset;
            public readonly int length;

            public MemorySlice(int offset, int length)
            {
                this.offset = offset;
                this.length = length;
            }
        }

        public void Dispose() => _fileReader.Dispose();
    }
}
