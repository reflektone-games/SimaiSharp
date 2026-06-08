using System;
using System.Collections.Generic;

namespace SimaiSharp.Structures
{
    [Serializable]
    public class SlidePath
    {
        public bool isBreak;
        public bool isMine;

        /// <summary>
        /// The intro delay of a slide before it starts moving.
        /// </summary>
        public float delay;

        public float duration;

        /// <summary>
        /// True if this slide path doesn't fade and scale up the star indicator during the delay.
        /// </summary>
        public bool noIntroAnimation;

        public List<int>          vertices = new();
        public List<SlideSegment> segments = new();
    }
}
