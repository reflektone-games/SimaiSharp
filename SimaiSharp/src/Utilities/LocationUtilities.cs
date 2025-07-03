using System.Runtime.CompilerServices;

namespace SimaiSharp.Utilities
{
    public static class LocationUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToNoteIndex(this int location) =>
            location & 0x0F;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToNoteGroup(this int location) =>
            (location & 0xF0) >> 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WithNoteGroup(this int location, int group) =>
            (location & 0x0F) | (group << 4);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WithNoteIndex(this int location, int index) =>
            (location & 0xF0) | index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTap(this int location) =>
            location.ToNoteGroup() == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTouch(this int location) =>
            location.ToNoteGroup() != 0;
    }
}
