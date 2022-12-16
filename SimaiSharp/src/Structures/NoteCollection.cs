using System;
using System.Collections.Generic;

namespace SimaiSharp.Structures
{
	[Serializable]
	public sealed class NoteCollection : List<Note>
	{
		public EachStyle eachStyle;
		public float     time;

		public NoteCollection(float time)
		{
			this.time = time;
		}

		public void AddNote(Note n)
		{
			n.parentCollection = this;
			Add(n);
		}
	}
}