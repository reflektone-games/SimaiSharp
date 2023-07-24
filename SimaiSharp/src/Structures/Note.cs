using System;
using System.Collections.Generic;
using System.Linq;

namespace SimaiSharp.Structures
{
	[Serializable]
	public struct Note
	{
		[NonSerialized] public NoteCollection parentCollection;

		public Location       location;
		public NoteStyles      styles;
		public NoteAppearance appearance;
		public NoteType       type;

		public float? length;

		public SlideMorph      slideMorph;
		public List<SlidePath> slidePaths;

		public Note(NoteCollection parentCollection)
		{
			this.parentCollection = parentCollection;
			slidePaths            = new List<SlidePath>();
			location              = default;
			styles                = NoteStyles.None;
			appearance            = NoteAppearance.Default;
			type                  = NoteType.Tap;
			length                = null;
			slideMorph            = SlideMorph.FadeIn;
		}

		public bool IsEx => (styles & NoteStyles.Ex) != 0;

		public bool IsStar => appearance >= NoteAppearance.ForceStar ||
		                      (slidePaths.Count > 0 && appearance is not NoteAppearance.ForceNormal);

		public float GetVisibleDuration()
		{
			var baseValue = length ?? 0;

			if (slidePaths is { Count: > 0 })
				baseValue = slidePaths.Max(s => s.delay + s.duration);

			return baseValue;
		}
	}
}
