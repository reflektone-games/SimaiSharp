using System;
using System.Collections.Generic;

namespace SimaiSharp.Structures
{
	[Serializable]
	public struct SlidePath
	{
		public Location           startLocation;
		public List<SlideSegment> segments;

		/// <summary>
		///     The intro delay of a slide before it starts moving.
		/// </summary>
		public float delay;

		public float duration;

		public NoteType type;

		public SlidePath(List<SlideSegment> segments)
		{
			this.segments = segments;
			startLocation = default;
			delay         = 0;
			duration      = 0;
			type          = NoteType.Slide;
		}
	}
}