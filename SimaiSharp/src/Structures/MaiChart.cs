using System;
using SimaiSharp.Internal.SyntacticAnalysis;

namespace SimaiSharp.Structures
{
	[Serializable]
	public sealed class MaiChart
	{
		public float?           FinishTiming;
		public NoteCollection[] NoteCollections = Array.Empty<NoteCollection>();
		public TimingChange[] TimingChanges = Array.Empty<TimingChange>();
	}
}
