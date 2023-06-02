using System;

namespace SimaiSharp.Structures
{
	[Serializable]
	public sealed class MaiChart
	{
		public float?           FinishTiming    { get; internal set; }
		public NoteCollection[] NoteCollections { get; internal set; } = Array.Empty<NoteCollection>();
	}
}