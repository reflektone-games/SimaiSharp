using System;
using System.Collections.Generic;
using SimaiSharp.Utilities;

namespace SimaiSharp.Structures
{
    [Serializable]
    public struct SlidePath
    {
        /// <summary>
        /// The intro delay of a slide before it starts moving.
        /// </summary>
        public float delay;

        public float duration;

        /// <summary>
        /// True if this slide path doesn't fade and scale up the star indicator during the delay.
        /// </summary>
        public bool noIntroAnimation;

        /// <summary>
        /// The locations of this slide path, represented in hexadecimal.
        /// </summary>
        /// <example>0x01 == Button 1</example>
        /// <example>0xA1 == Touch A1</example>
        /// <example>0xC0 == Touch C</example>
        /// <remarks>Use <see cref="LocationUtilities"/> to get the index and group.</remarks>
        public List<int> vertices;

        public List<SlideType> segmentTypes;
    }
}
