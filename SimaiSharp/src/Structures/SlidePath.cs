using System.Collections.Generic;

namespace SimaiSharp.Structures
{
    public sealed class SlidePath
    {
        public int index;

        public bool isBreak;
        public bool isMine;

        public double time;

        /// <summary>
        /// The intro delay of a slide before it starts moving.
        /// </summary>
        public double delay;

        public double duration;

        /// <summary>
        /// True if this slide path doesn't fade and scale up the star indicator during the delay.
        /// </summary>
        public bool noIntroAnimation;

        public List<int>          vertices = [];
        public List<SlideSegment> segments = [];
    }
}
