using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using SimaiSharp.Internal.Errors;
using SimaiSharp.Internal.LexicalAnalysis;

namespace SimaiSharp.Internal.SyntacticAnalysis.States
{
	internal static class SubdivisionReader
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Process(Deserializer parent, Token token)
		{
			if (token.lexeme.Span[0] == '#')
			{
				if (!float.TryParse(token.lexeme.Span[1..],
				                    NumberStyles.Any,
				                    CultureInfo.InvariantCulture,
				                    out var explicitTempo))
					throw new UnexpectedCharacterException(token.line, token.character + 1, "0~9, or \".\"");

				var newTimingChange = parent.timingChanges.Last.Value;
				newTimingChange.SetSeconds(explicitTempo);
				newTimingChange.time = parent.currentTime;

				if (Math.Abs(parent.timingChanges.Last.Value.time - parent.currentTime) <= float.Epsilon)
					parent.timingChanges.RemoveLast();

				parent.timingChanges.AddLast(newTimingChange);
				return;
			}

			if (!float.TryParse(token.lexeme.Span,
								NumberStyles.Any,
								CultureInfo.InvariantCulture,
								out var subdivision)) throw new UnexpectedCharacterException(token.line, token.character, "0~9, or \".\"");

			{
				var newTimingChange = parent.timingChanges.Last.Value;
				newTimingChange.subdivisions = subdivision;
				newTimingChange.time = parent.currentTime;

				if (Math.Abs(parent.timingChanges.Last.Value.time - parent.currentTime) <= float.Epsilon)
					parent.timingChanges.RemoveLast();

				parent.timingChanges.AddLast(newTimingChange);
			}
		}
	}
}
