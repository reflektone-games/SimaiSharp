using System.Collections.Generic;

namespace SimaiSharp.Structures
{
	public struct SlideSegment
	{
		/// <summary>
		///     Describes the target buttons
		/// </summary>
		// ReSharper disable once FieldCanBeMadeReadOnly.Global
		public List<Location> vertices;

		public SlideType slideType;

		public SlideSegment(List<Location>? vertices = null)
		{
			this.vertices = vertices ?? new List<Location>();
			slideType     = SlideType.StraightLine;
		}
	}
}