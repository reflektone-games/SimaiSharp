using System;

namespace SimaiSharp.Structures
{
	// ReSharper disable once StructCanBeMadeReadOnly
	public struct Location
	{
		/// <summary>
		///     Describes which button / sensor in the <see cref="group" /> this element is pointing to.
		/// </summary>
		public int index;

		public NoteGroup group;

		public Location(int index, NoteGroup group)
		{
			this.index = index;
			this.group = group;
		}

		public static bool operator ==(Location x, Location y)
		{
			return x.group == y.group && x.index == y.index;
		}

		public static bool operator !=(Location x, Location y)
		{
			return x.group != y.group || x.index != y.index;
		}

		public bool Equals(Location other)
		{
			return index == other.index && group == other.group;
		}

		public override bool Equals(object? obj)
		{
			return obj is Location other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(index, (int)group);
		}

		public override string ToString()
		{
			return $"{nameof(index)}: {index}, {nameof(group)}: {group}";
		}
	}
}