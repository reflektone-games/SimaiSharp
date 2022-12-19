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

		public float? length;

		public List<SlidePath>? slidePaths;

		public Note(NoteCollection parentCollection)
		{
			this.parentCollection = parentCollection;
			slidePaths            = new();
			location              = default;
			styles                = NoteStyle.None;
			type                  = NoteType.Tap;
			length                = null;
		}

		public bool IsEx => (styles & NoteStyle.Ex) != 0;

		public float GetVisibleDuration()
		{
			var baseValue = length ?? 0;

			if (slidePaths.Count > 0)
				baseValue += slidePaths.Max(s => s.duration);

			return baseValue;
		}
	}
}