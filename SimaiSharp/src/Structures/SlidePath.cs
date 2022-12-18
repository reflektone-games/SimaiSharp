using System;
using System.Collections.Generic;

namespace SimaiSharp.Structures
{
	[Serializable]
	public struct SlidePath
	{
		public int                startVertex;
		public List<SlideSegment> segments;

		public float introDuration;
		public float duration;

		public SlideMorph slideMorph;
	}
}