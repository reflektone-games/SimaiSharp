using System;
using SimaiSharp.Utilities;

namespace SimaiSharp.Structures
{
    [Serializable]
    public struct Note
    {
        /// <summary>
        /// The duration of this note, in seconds.
        /// </summary>
        public float length;

        /// <summary>
        /// Groups multiple notes together.
        /// Use this field to determine the color of the note, as well as drawing connectors between notes.
        /// If this field is negative, then notes inside this group should always be displayed as "each" notes.
        /// </summary>
        public int eachGroup;

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
