using System;

namespace SimaiSharp.Structures
{
    [Flags]
    public enum NoteStyles
    {
        None      = 0,
        Hold      = 1 << 0,
        Star      = 1 << 1,
        Spinning  = 1 << 2,
        Ex        = 1 << 3,
        Fireworks = 1 << 4,
        Mine      = 1 << 5,
    }
}
