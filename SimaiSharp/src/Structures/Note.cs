using System;
using SimaiSharp.Utilities;

namespace SimaiSharp.Structures
{
    public sealed class Note
    {
        /// <summary>
        /// The duration of this note, in seconds.
        /// </summary>
        public float length;

        /// <summary>
        /// What category this note should be counted as in results.
        /// </summary>
        public NoteCategory category;

        /// <summary>
        /// Visual changes applied to this note.
        /// </summary>
        /// <remarks>
        /// This doesn't affect the category nor its behavior.
        /// Identify a note's behavior with <see cref="length"/>.
        /// </remarks>
        public NoteStyles styles;

        /// <summary>
        /// The location of this note, represented in hexadecimal.
        /// </summary>
        /// <example>0x00 == Button 1</example>
        /// <example>0xA1 == Touch A2</example>
        /// <example>0xC0 == Touch C</example>
        /// <remarks>Use <see cref="LocationUtilities"/> to get the index and group.</remarks>
        public int location;
    }
}
