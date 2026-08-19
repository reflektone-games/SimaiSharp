using System.Collections.Generic;

namespace SimaiSharp.Structures
{
    public sealed class SimaiChart
    {
        public double          finishTiming;
        public List<NoteGroup> noteGroups = [];
        public ulong           hash;
    }
}
