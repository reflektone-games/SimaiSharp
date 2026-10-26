using System;
using System.Globalization;
using System.Text;

namespace SimaiSharp.Utilities
{
    public static class MemoryUtilities
    {
        public static ReadOnlySpan<byte> TrimEnd(this ReadOnlySpan<byte> span)
        {
            var end = span.Length - 1;
            for (; end >= 0; end--)
            {
                if (span[end] is not ((byte)' ' or (byte)'\n' or (byte)'\r'))
                    break;
            }

            return span[..(end + 1)];
        }

        public static bool TryParseFloat(this ReadOnlySpan<byte> utf8Bytes, out float value)
        {
            // The resulting char array will have a maximum length of utf8Bytes.Length,
            // But a mismatch may happen due to code points.
            Span<char>         chars     = stackalloc char[utf8Bytes.Length];
            var                charCount = Encoding.UTF8.GetChars(utf8Bytes, chars);
            ReadOnlySpan<char> charSpan  = chars[..charCount];
            return float.TryParse(charSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
