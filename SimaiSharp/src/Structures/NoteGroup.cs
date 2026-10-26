using System.Collections.Generic;

namespace SimaiSharp.Structures
{
    public sealed class NoteGroup
    {
        public double startTime;
        public double endTime;

        public List<Note>      notes      = [];
        public List<SlidePath> slidePaths = [];

        public List<State> paceChanges  = [];
        public List<State> scaleChanges = [];

        public bool IsEmpty => notes.Count == 0 && slidePaths.Count == 0;
    }
}
