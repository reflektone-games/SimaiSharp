using System;
using System.Collections.Generic;
using System.Linq;

namespace SimaiSharp.Structures
{
	[Serializable]
	public struct Note
	{
		[NonSerialized] public NoteCollection parentCollection;

		public Location  location;
		public NoteStyle styles;
		public NoteType  type;

		public float length;

		public List<SlidePath> slidePaths;

		public bool IsEx => (styles & NoteStyle.Ex) != 0;

		public float GetVisibleDuration()
		{
			var baseValue = length;

			if (slidePaths.Count > 0)
				baseValue += slidePaths.Max(s => s.duration);

			return baseValue;
		}
	}
}