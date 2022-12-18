using SimaiSharp.Internal.LexicalAnalysis;

namespace SimaiSharp.Internal.Parsing.States
{
	internal static class SubdivisionReader
	{
		public static void Process(Deserializer parent, Token token)
		{
			if (token.lexeme.Span[0] == '#')
			{
				if (!float.TryParse(token.lexeme.Span[1..], out var explicitTempo))
					throw ErrorHandler.DeserializationError(token, $"Explicit tempo includes non-numeric value.");
				
				parent.currentTiming.ExplicitOverride(explicitTempo);
				return;
			}

			if (!float.TryParse(token.lexeme.Span, out var subdivision))
				throw ErrorHandler.DeserializationError(token, $"Subdivision includes non-numeric value.");
			
			parent.currentTiming.subdivisions = subdivision;
		}
	}
}