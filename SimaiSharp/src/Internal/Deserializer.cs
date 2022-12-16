using System;
using SimaiSharp.Structures;

namespace SimaiSharp.Internal
{
	internal class Deserializer : SequenceAnalyzer<Token>
	{
		public Deserializer(Token[] sequence) : base(sequence)
		{
		}

		private readonly MaiChart _chart = new();

		public MaiChart GetChart()
		{
			var tokenSpan = new ReadOnlySpan<Token>(sequence);

			var time = 0f;
			while (!IsAtEnd)
			{
				start = current;
				InterpretToken(in tokenSpan, ref time);
			}

			return _chart;
		}

		private void InterpretToken(in ReadOnlySpan<Token> sourceSpan, ref float time)
		{
			item++;
			var c = Advance(sourceSpan);
			switch (c.type)
			{
				case TokenType.Tempo:
					break;
				case TokenType.Subdivision:
					break;
				case TokenType.Location:
					break;
				case TokenType.Decorator:
					break;
				case TokenType.Slide:
					break;
				case TokenType.Duration:
					break;
				case TokenType.SlideJoiner:
					
					break;
				case TokenType.TimeStep:
					time += _chart.timingChanges.Count > 0 ? _chart.timingChanges[^1].SecondsPerBeat : 0;
					break;
				case TokenType.EachDivider:
					break;
				case TokenType.EndOfFile:
					_chart.finishTiming = time;
					break;
				default:
					ErrorHandler.Report(line, item, c.ToString(), "Unexpected token.");
					break;
			}
		}
	}
}