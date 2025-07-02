using System.Collections.Generic;

namespace SimaiSharp.Structures
{
    public sealed class NoteFrame
    {
        public float time;
        public bool  isEach;

        public IReadOnlyList<Note>      Notes      => notes;
        public IReadOnlyList<SlidePath> SlidePaths => slidePaths;

        internal readonly List<Note>      notes      = new(2);
        internal readonly List<SlidePath> slidePaths = new();
    }
}
