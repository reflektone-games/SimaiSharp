using System.Collections.Generic;

namespace SimaiSharp.Structures
{
    public struct NoteFrame
    {
        public float time;

        public readonly IReadOnlyList<Note>      Notes      => _notes;
        public readonly IReadOnlyList<SlidePath> SlidePaths => _slidePaths;

        internal readonly List<Note>      _notes;
        internal readonly List<SlidePath> _slidePaths;

        public NoteFrame(float time)
        {
            _notes      = new List<Note>();
            _slidePaths = new List<SlidePath>();
            this.time   = time;
        }
    }
}
