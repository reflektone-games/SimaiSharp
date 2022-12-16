using System;

namespace SimaiSharp.Structures
{
	[Flags]
	public enum NoteStyle
	{
		None      = 0,
		Star      = 1 << 0,
		Ex        = 1 << 1,
		Fireworks = 1 << 2,
	}
}