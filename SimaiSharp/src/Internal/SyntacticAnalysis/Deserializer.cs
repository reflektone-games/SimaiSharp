using System;
using System.Collections.Generic;
using SimaiSharp.Internal.LexicalAnalysis;
using SimaiSharp.Internal.SyntacticAnalysis.States;
using SimaiSharp.Structures;

namespace SimaiSharp.Internal.SyntacticAnalysis
{
	internal class Deserializer : IDisposable
	{
		internal readonly IEnumerator<Token> enumerator;
		private readonly  MaiChart           _chart = new();
		internal          TimingChange       currentTiming;
		internal          NoteCollection?    currentNoteCollection;
		private           float              _currentTime;
		internal          bool               endOfFile;

		public Deserializer(IEnumerable<Token> sequence)
		{
			enumerator            = sequence.GetEnumerator();
			currentTiming         = new TimingChange();
			currentNoteCollection = null;
			_currentTime          = 0;
			endOfFile             = false;
		}

		public void Dispose()
		{
			enumerator.Dispose();
		}

		public MaiChart GetChart()
		{
			// Some readers (e.g. NoteReader) moves the enumerator automatically.
			// We can skip moving the pointer if that's satisfied.
			var manuallyMoved = false;
			
			while (!endOfFile && (manuallyMoved || MoveNext()))
			{
				var token = enumerator.Current;
				manuallyMoved = false;

				switch (token.type)
				{
					case TokenType.Tempo:
					{
						TempoReader.Process(this, token);
						break;
					}
					case TokenType.Subdivision:
					{
						SubdivisionReader.Process(this, token);
						break;
					}
					case TokenType.Location:
					{
						currentNoteCollection ??= new NoteCollection(_currentTime);

						if (token.lexeme.Span[0] == '0')
						{
							if (currentNoteCollection.eachStyle is not EachStyle.ForceBroken)
								currentNoteCollection.eachStyle = EachStyle.ForceEach;
							break;
						}

						var note = NoteReader.Process(this, token);
						currentNoteCollection.AddNote(ref note);
						manuallyMoved = true;
						break;
					}
					case TokenType.TimeStep:
					{
						if (currentNoteCollection != null)
						{
							_chart.AddCollection(currentNoteCollection);
							currentNoteCollection = null;
						}

						_currentTime += currentTiming.SecondsPerBeat;
					}
						break;
					case TokenType.EachDivider:
						break;
					case TokenType.Decorator:
						throw ErrorHandler.DeserializationError(token, "Decorators should be attached to notes.");
					case TokenType.Slide:
						throw ErrorHandler.DeserializationError(token, "Slides should be attached to notes.");
					case TokenType.Duration:
						throw ErrorHandler.DeserializationError(token,
						                                        "Duration should be either attached to notes, or slides.");
					case TokenType.SlideJoiner:
						throw ErrorHandler.DeserializationError(token, "Slide joiners should be attached to slides.");
					case TokenType.EndOfFile:
						_chart.finishTiming = _currentTime;
						break;
					case TokenType.None:
						break;
					default:
						throw ErrorHandler.DeserializationError(token, "Unexpected token.");
				}
			}

			if (currentNoteCollection == null)
				return _chart;
			
			_chart.AddCollection(currentNoteCollection);
			currentNoteCollection = null;
			return _chart;
		}

		/// <param name="startLocation">The starting location.</param>
		/// <param name="endLocation">The ending location.</param>
		/// <param name="direction">1: Right; -1: Left; Default: Shortest route.</param>
		/// <returns>The recommended ring type</returns>
		internal static SlideType DetermineRingType(Location startLocation,
		                                            Location endLocation,
		                                            int      direction = 0)
		{
			switch (direction)
			{
				case 1:
					return (startLocation.index + 2) % 8 >= 4 ? SlideType.RingCcw : SlideType.RingCw;
				case -1:
					return (startLocation.index + 2) % 8 >= 4 ? SlideType.RingCw : SlideType.RingCcw;
				default:
				{
					var difference = endLocation.index - startLocation.index;

					var rotation = difference >= 0 ? difference > 4 ? -1 : 1 :
					               difference < -4 ? 1 : -1;

					return rotation > 0 ? SlideType.RingCw : SlideType.RingCcw;
				}
			}
		}

		internal static bool TryReadLocation(in Token token, out Location value)
		{
			value = new Location();

			var isSensor = token.lexeme.Span[0] is >= 'A' and <= 'E';
			var indexRange = isSensor
				                 ? token.lexeme.Span[1..]
				                 : token.lexeme.Span[..];

			if (isSensor)
			{
				value.group = (NoteGroup)(token.lexeme.Span[0] - 'A' + 1);

				if (value.group == NoteGroup.CSensor)
					return true;
			}

			if (!int.TryParse(indexRange, out var indexInGroup)) return false;

			// Convert from 1-indexed to 0-indexed
			value.index = indexInGroup - 1;
			return true;
		}

		public bool MoveNext()
		{
			return !(endOfFile = !enumerator.MoveNext());
		}
	}
}