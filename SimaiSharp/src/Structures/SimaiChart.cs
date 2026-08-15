using System.Collections.Generic;

namespace SimaiSharp.Structures
{
    public class SimaiChart
    {
        public float             finishTiming;
        public List<NoteFrame>   noteFrames   = new();
        public List<TempoChange> tempoChanges = new();
        public ulong               hash;
    }
}
