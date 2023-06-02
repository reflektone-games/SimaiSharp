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

				parent.currentTiming.ExplicitOverride(explicitTempo);
				return;
			}

			if (!float.TryParse(token.lexeme.Span,
			                    NumberStyles.Any,
			                    CultureInfo.InvariantCulture,
			                    out var subdivision))
				throw new UnexpectedCharacterException(token.line, token.character, "0~9, or \".\"");

			parent.currentTiming.subdivisions = subdivision;
		}
	}
}