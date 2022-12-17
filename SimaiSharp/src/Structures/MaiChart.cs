using System;
using System.Collections.Generic;

namespace SimaiSharp.Structures
{
	[Serializable]
	public sealed class MaiChart
	{
		public List<NoteCollection> noteCollections = new();
		public float?               finishTiming;
	}
}