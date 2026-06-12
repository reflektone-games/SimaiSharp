using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SimaiSharp.FileReading
{
    public sealed class SimaiFileReader(IFileReader fileReader) : IDisposable
    {
        private bool FullyScannedFile => _nextUnreadByteIndex >= GetByteSpan().Length;

        private readonly Dictionary<int, MemorySlice> _entries = new();
        private          int                          _nextUnreadByteIndex;

        public static SimaiFileReader FromPath(string   path)   => new(IFileReader.Create(path));
        public static SimaiFileReader FromStream(Stream stream) => new(IFileReader.Create(stream));

        public ReadOnlySpan<byte> GetByteSpan() => fileReader.GetSpan();

        /// <returns>A boolean indicating whether to decode the value</returns>
        public delegate void OnEntryRead(string key, ReadOnlySpan<byte> value);

        public void ScanFile(int? stopAtKey = null)
        {
            var bytes = GetByteSpan();

            var  keyHash    = 0;
            var  keyStart   = 0;
            var  valueStart = 0;
            var  readingKey = false;
            byte lastByte   = 0;
            int  byteIndex;

            for (byteIndex = _nextUnreadByteIndex; byteIndex < bytes.Length; byteIndex++)
            {
                var currentByte = bytes[byteIndex];

                switch (currentByte)
                {
                    case (byte)'&' when lastByte is (byte)'\0' or (byte)'\n' or (byte)'\r':
                    {
                        if (keyStart < valueStart) // consider valueStart != 0
                        {
                            if (stopAtKey == keyHash)
                                goto FINALIZE;

                            _entries[keyHash] = new MemorySlice(valueStart, byteIndex - valueStart);
                        }

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
                _entries[keyHash] = new MemorySlice(valueStart, byteIndex - valueStart);

            _nextUnreadByteIndex = byteIndex;
        }

        public void Enumerate(OnEntryRead onEntryRead)
        {
            var bytes = GetByteSpan();

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

        public string GetValue(string key) =>
            TryGetValue(key, out var result)
                ? result
                : throw new KeyNotFoundException($"Key {key} is not defined in this file.");

        public ReadOnlySpan<byte> GetValueSpan(string key) =>
            TryGetValueSpan(key, out var result)
                ? result
                : throw new KeyNotFoundException($"Key {key} is not defined in this file.");

        public bool TryGetValue(string key, out string value)
        {
            if (TryGetValueSpan(key, out var resultSpan))
            {
                value = Encoding.UTF8.GetString(resultSpan);
                return true;
            }

            value = string.Empty;
            return false;
        }

        public bool TryGetValueSpan(string key, out ReadOnlySpan<byte> result)
        {
            var keyHash = ComputeHash(key);

            if (_entries.TryGetValue(keyHash, out var entry))
            {
                result = GetByteSpan().Slice(entry.offset, entry.length);
                return true;
            }

            if (FullyScannedFile)
            {
                result = null;
                return false;
            }

            ScanFile(keyHash);

            if (_entries.TryGetValue(keyHash, out entry))
            {
                result = GetByteSpan().Slice(entry.offset, entry.length);
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// https://stackoverflow.com/questions/7956167/how-can-i-quickly-read-bytes-from-a-memory-mapped-file-in-net
        /// </summary>
        public string GetString(ReadOnlySpan<byte> bytes, MemorySlice slice) =>
            Encoding.UTF8.GetString(bytes.Slice(slice.offset, slice.length));

        /// <summary>
        /// https://stackoverflow.com/questions/16340/how-do-i-generate-a-hashcode-from-a-byte-array-in-c
        /// </summary>
        public static int ComputeHash(ReadOnlySpan<byte> bytes)
        {
            const int p = 16777619;
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var @byte in bytes)
                    hash = (hash ^ @byte) * p;
                return hash;
            }
        }

        public static int ComputeHash(ReadOnlySpan<char> chars)
        {
            const int p = 16777619;
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var @char in chars)
                    for (var c = @char; c > 0; c >>= 8)
                        hash = (hash ^ (c & 0xFF)) * p;
                return hash;
            }
        }

        public readonly struct MemorySlice(int offset, int length)
        {
            public readonly int offset = offset;
            public readonly int length = length;
        }

        public void Dispose() => fileReader.Dispose();
    }
}
