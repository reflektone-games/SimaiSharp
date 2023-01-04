using System.Globalization;
using SimaiSharp.Internal.LexicalAnalysis;

namespace SimaiSharp.Internal.SyntacticAnalysis.States
{
	internal static class TempoReader
	{
		public static void Process(Deserializer parent, Token token)
		{
			if (!float.TryParse(token.lexeme.Span,
			                    NumberStyles.Any,
			                    CultureInfo.InvariantCulture,
			                    out var tempo))
				throw ErrorHandler.DeserializationError(token, "Tempo includes non-numeric value.");

			parent.currentTiming.tempo = tempo;
		}
	}
}