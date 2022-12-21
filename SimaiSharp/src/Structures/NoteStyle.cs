using System;

namespace SimaiSharp.Structures
{
	[Flags]
	public enum NoteStyle
	{
		None      = 0,
		Ex        = 1 << 0,
		Fireworks = 1 << 1,
	}
}