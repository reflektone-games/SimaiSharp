using System.Runtime.CompilerServices;

namespace SimaiSharp.Utilities
{
    public static class LocationUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToNoteIndex(this int location) =>
            location & 0x0F;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToNoteGroupIndex(this int location) =>
            location & 0xF0 >> 4;
    }
}
