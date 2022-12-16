using System;
using System.Collections.Generic;

namespace SimaiSharp.Structures
{
	[Serializable]
	public sealed class MaiChart
	{
		public float? finishTiming;

		public List<NoteCollection> noteCollections = new();
		public List<TimingChange>   timingChanges   = new();
	}
}