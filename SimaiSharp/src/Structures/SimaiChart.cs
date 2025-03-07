using System;
using System.Collections.Generic;

namespace SimaiSharp.Structures
{
    [Serializable]
    public struct SimaiChart
    {
        public float             finishTiming;
        public List<NoteFrame>   noteFrames;
        public List<TempoChange> tempoChanges;
    }
}
