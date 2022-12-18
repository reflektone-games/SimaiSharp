using System;

namespace SimaiSharp.Structures
{
	public struct Location
	{
		/// <summary>
		/// Describes which button / sensor in the <see cref="group"/> this element is pointing to.
		/// </summary>
		public int index;

		public NoteGroup group;

		public override string ToString()
		{
			return group switch
			       {
				       NoteGroup.Tap     => index.ToString(),
				       NoteGroup.ASensor => "A" + index,
				       NoteGroup.BSensor => "B" + index,
				       NoteGroup.CSensor => "C" + index,
				       NoteGroup.DSensor => "D" + index,
				       NoteGroup.ESensor => "E" + index,
				       _                 => throw new ArgumentOutOfRangeException()
			       };
		}
	}
}