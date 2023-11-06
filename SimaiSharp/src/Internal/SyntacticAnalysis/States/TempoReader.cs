using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml.XPath;
using SimaiSharp.Internal.Errors;
using SimaiSharp.Internal.LexicalAnalysis;

namespace SimaiSharp.Internal.SyntacticAnalysis.States
{
	internal static class TempoReader
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Process(Deserializer parent, Token token)
		{
			if (!float.TryParse(token.lexeme.Span,
			                    NumberStyles.Any,
			                    CultureInfo.InvariantCulture,
			                    out var tempo))
				throw new UnexpectedCharacterException(token.line, token.character, "0~9, or \".\"");

			var newTimingChange = parent.timingChanges.Last.Value;
			newTimingChange.tempo = tempo;
			newTimingChange.time = parent.currentTime;

			if (Math.Abs(parent.timingChanges.Last.Value.time - parent.currentTime) <= float.Epsilon)
				parent.timingChanges.RemoveLast();

			parent.timingChanges.AddLast(newTimingChange);
		}
	}
}
