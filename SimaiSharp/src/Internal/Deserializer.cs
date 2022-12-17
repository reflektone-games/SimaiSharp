using System.Collections.Generic;
using SimaiSharp.Structures;

namespace SimaiSharp.Internal
{
	internal class Deserializer
	{
		private readonly IEnumerable<Token> _sequence;
		private readonly MaiChart           _chart = new();

		public Deserializer(IEnumerable<Token> sequence)
		{
			_sequence = sequence;
		}

		public MaiChart GetChart()
		{
			var time = 0f;
			
			var currentTiming = new TimingChange();

			foreach (var token in _sequence)
			{
				switch (token.type)
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
						time += currentTiming.SecondsPerBeat;
						break;
					case TokenType.EachDivider:
						break;
					case TokenType.EndOfFile:
						_chart.finishTiming = time;
						break;
					default:
						ErrorHandler.DeserializationError(token, "Unexpected token.");
						break;
				}
			}

			return _chart;
		}
	}
}