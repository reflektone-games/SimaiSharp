using System.Collections.Generic;

namespace SimaiSharp.Structures
{
    public sealed class NoteGroup
    {
        public double            startTime;
        public double            endTime;
        public List<Note>        notes                  = [];
        public List<SlidePath>   slidePaths             = [];
        public List<SpeedChange> speedVariationChanges  = [];
        public List<SpeedChange> speedMultiplierChanges = [];
    }
}
