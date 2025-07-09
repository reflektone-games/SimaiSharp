using System;

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
    }
}
