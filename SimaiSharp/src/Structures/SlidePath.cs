using System;
using System.Collections.Generic;

namespace SimaiSharp.Structures
{
	[Serializable]
	public struct SlidePath
	{
		/// <summary>
		/// Describes the target buttons 
		/// </summary>
		public List<int> vertices;
		public SlideType slideType;

		public float introDuration;
		public float duration;

		public SlideMorph slideMorph;

		public SlidePath(SlideType slideType,
		                 List<int> vertices,
		                 float     introDuration,
		                 float     duration)
		{
			this.slideType = slideType;

			this.vertices      = vertices;
			this.introDuration = introDuration;
			this.duration      = duration;
			slideMorph         = SlideMorph.Original;
		}
	}
}