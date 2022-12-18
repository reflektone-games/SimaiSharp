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

			var             currentTiming         = new TimingChange();
			NoteCollection? currentNoteCollection = null;
			Note            currentNote           = default;
			var             emptyNote             = true;

			using var enumerator = _sequence.GetEnumerator();

			while (enumerator.MoveNext())
			{
				var token = enumerator.Current;

				switch (token.type)
				{
					case TokenType.Tempo:
					{
						if (float.TryParse(token.lexeme.Span, out var tempo))
						{
							currentTiming.tempo = tempo;
							continue;
						}

						ErrorHandler.DeserializationError(token, $"Tempo includes non-numeric value.");
						break;
					}
					case TokenType.Subdivision:
					{
						if (token.lexeme.Span[0] == '#')
						{
							if (float.TryParse(token.lexeme.Span[1..], out var explicitTempo))
							{
								currentTiming.ExplicitOverride(explicitTempo);
								continue;
							}

							ErrorHandler.DeserializationError(token, $"Explicit tempo includes non-numeric value.");
							continue;
						}

						if (float.TryParse(token.lexeme.Span, out var subdivision))
						{
							currentTiming.subdivisions = subdivision;
							continue;
						}

						ErrorHandler.DeserializationError(token, $"Subdivision includes non-numeric value.");
					}
						break;
					case TokenType.Location:
					{
						currentNoteCollection ??= new NoteCollection(time);

						if (!emptyNote)
						{
							currentNoteCollection.AddNote(currentNote);
						}

						emptyNote   = false;
						currentNote = new Note();

						var location = ReadLocation(token);

						if (location.HasValue)
							currentNote.location = location.Value;
					}
						break;
					case TokenType.Decorator:
						break;
					case TokenType.Slide:
						break;
					case TokenType.Duration:
						break;
					case TokenType.SlideJoiner:
						break;
					case TokenType.EachDivider:
					{
						currentNoteCollection?.AddNote(currentNote);
						emptyNote = true;
					}
						break;
					case TokenType.TimeStep:
					{
						if (!emptyNote)
						{
							currentNoteCollection?.AddNote(currentNote);
							emptyNote = true;
						}

						if (currentNoteCollection != null)
						{
							_chart.AddCollection(currentNoteCollection);
							currentNoteCollection = null;
							emptyNote             = true;
						}

						time += currentTiming.SecondsPerBeat;
					}
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

		private Location? ReadLocation(Token token)
		{
			var retVal = new Location();

			var isSensor = token.lexeme.Span[0] is >= 'A' and <= 'E';
			var indexRange = isSensor
				                 ? token.lexeme.Span[1..]
				                 : token.lexeme.Span[..];

			if (isSensor)
			{
				retVal.group = (NoteGroup)(token.lexeme.Span[0] - 'A' + 1);

				if (retVal.group == NoteGroup.CSensor)
					return retVal;
			}

			if (int.TryParse(indexRange, out var indexInGroup))
			{
				retVal.index = indexInGroup;
				return retVal;
			}

			ErrorHandler.DeserializationError(token, $"Invalid location declaration.");
			return null;
		}
	}
}