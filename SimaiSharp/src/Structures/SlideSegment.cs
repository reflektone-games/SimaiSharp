using System.Collections.Generic;

namespace SimaiSharp.Structures
{
	public struct SlideSegment
	{
		/// <summary>
		/// Describes the target buttons 
		/// </summary>
		public readonly List<Location> vertices;

		public SlideType slideType;
		public bool      isBreak;

		public SlideSegment(List<Location>? vertices = null)
		{
			this.vertices = vertices ?? new List<Location>();
			slideType     = SlideType.StraightLine;
			isBreak       = false;
		}
	}
}