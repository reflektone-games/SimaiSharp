using SimaiSharp.Internal.LexicalAnalysis;
using SimaiSharp.Internal.SyntacticAnalysis;
using SimaiSharp.Structures;

namespace SimaiSharp
{
	public static class SimaiConvert
	{
		public static MaiChart Deserialize(string value)
		{
			var tokens = new Tokenizer(value).GetTokens();
			var chart  = new Deserializer(tokens).GetChart();

			return chart;
		}
	}
}