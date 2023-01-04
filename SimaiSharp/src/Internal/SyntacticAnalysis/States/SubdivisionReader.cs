using System.Globalization;
using System.Runtime.CompilerServices;
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
					throw ErrorHandler.DeserializationError(token, "Explicit tempo includes non-numeric value.");

				parent.currentTiming.ExplicitOverride(explicitTempo);
				return;
			}

			if (!float.TryParse(token.lexeme.Span,
			                    NumberStyles.Any,
			                    CultureInfo.InvariantCulture,
			                    out var subdivision))
				throw ErrorHandler.DeserializationError(token, "Subdivision includes non-numeric value.");

			parent.currentTiming.subdivisions = subdivision;
		}
	}
}