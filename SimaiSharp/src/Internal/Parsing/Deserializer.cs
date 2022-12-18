﻿using System;
using System.Collections.Generic;
using SimaiSharp.Internal.LexicalAnalysis;
using SimaiSharp.Internal.Parsing.States;
using SimaiSharp.Structures;

namespace SimaiSharp.Internal.Parsing
{
	internal class Deserializer : IDisposable
	{
		internal readonly IEnumerator<Token> enumerator;
		private readonly  MaiChart           _chart = new();
		internal          TimingChange       currentTiming;
		internal          NoteCollection?    currentNoteCollection;
		internal          float              currentTime;

		public Deserializer(IEnumerable<Token> sequence)
		{
			enumerator            = sequence.GetEnumerator();
			currentTiming         = new TimingChange();
			currentNoteCollection = null;
			currentTime           = 0;
		}

		public MaiChart GetChart()
		{
			// Some readers (e.g. NoteReader) moves the enumerator automatically.
			// We can skip moving the pointer if that's satisfied.
			var alreadyMoved = false;

			while (alreadyMoved || enumerator.MoveNext())
			{
				var token = enumerator.Current;
				alreadyMoved = false;

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
						NoteReader.Process(this, token);
						alreadyMoved = true;
						break;
					}
					case TokenType.Decorator:
						throw ErrorHandler.DeserializationError(token, "Decorators should be attached to notes.");
					case TokenType.Slide:
						throw ErrorHandler.DeserializationError(token, "Slides should be attached to notes.");
					case TokenType.Duration:
						throw ErrorHandler.DeserializationError(token,
						                                        "Duration should be either attached to notes, or slides.");
					case TokenType.SlideJoiner:
						throw ErrorHandler.DeserializationError(token, "Slide joiners should be attached to slides.");
					case TokenType.EachDivider:
						throw ErrorHandler.DeserializationError(token, "Each dividers should be attached to notes.");
					case TokenType.TimeStep:
					{
						if (currentNoteCollection != null)
						{
							_chart.AddCollection(currentNoteCollection);
							currentNoteCollection = null;
						}

						currentTime += currentTiming.SecondsPerBeat;
					}
						break;
					case TokenType.EndOfFile:
						_chart.finishTiming = currentTime;
						break;
					default:
						throw ErrorHandler.DeserializationError(token, "Unexpected token.");
				}
			}

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

		internal static bool TryReadLocation(Token token, out Location value)
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

		public void Dispose()
		{
			enumerator.Dispose();
		}
	}
}