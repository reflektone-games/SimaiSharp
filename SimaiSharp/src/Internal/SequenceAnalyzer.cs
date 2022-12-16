using System;

namespace SimaiSharp.Internal
{
	internal abstract class SequenceAnalyzer<T>
	{
		protected readonly T[] sequence;
		
		protected int start;
		protected int current;
		protected int item;
		protected int line = 1;
		
		/// <summary>
		/// Returns the <see cref="current"/> glyph, and increments by one.
		/// </summary>
		protected T Advance(in ReadOnlySpan<T> sourceSpan) =>
			sourceSpan[current++];

		/// <summary>
		/// Returns the <see cref="current"/> glyph without incrementing.
		/// </summary>
		protected T? Peek(in ReadOnlySpan<T?> sourceSpan) =>
			IsAtEnd ? default : sourceSpan[current];

		/// <summary>
		/// Returns the last glyph without decrementing.
		/// </summary>
		protected T? PeekPrevious(in ReadOnlySpan<T?> sourceSpan) =>
			current == 0 ? default : sourceSpan[current - 1];
		
		protected bool IsAtEnd => current >= sequence.Length;

		public SequenceAnalyzer(T[] sequence)
		{
			this.sequence = sequence;
		}
	}
}