namespace SimaiSharp.Structures
{
	public struct Location
	{
		/// <summary>
		/// Describes which button / sensor in the <see cref="group"/> this element is pointing to.
		/// </summary>
		public int index;

		public NoteGroup group;

		public Location(NoteGroup group, int index)
		{
			this.group = group;
			this.index = index;
		}
	}
}