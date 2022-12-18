using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SimaiSharp.Internal.LexicalAnalysis;
using SimaiSharp.Structures;

namespace SimaiSharp.Internal.SyntacticAnalysis.States
{
	internal static class NoteReader
	{
		public static void Process(Deserializer parent, Token token)
		{
			parent.currentNoteCollection ??= new NoteCollection(parent.currentTime);

			if (!Deserializer.TryReadLocation(token, out var noteLocation))
				throw ErrorHandler.DeserializationError(token, $"Invalid location declaration.");

			var currentNote = new Note
			                  {
				                  location = noteLocation
			                  };

			while (parent.enumerator.MoveNext())
			{
				var decorationToken = parent.enumerator.Current;

				switch (decorationToken.type)
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
					{
						var validSlideString =
							MemoryMarshal.TryGetString(token.lexeme, out var text, out var start, out var length);

						if (!validSlideString)
						{
							throw ErrorHandler.DeserializationError(token, "Failed to get string from slide literal.");
						}

						var segment   = new SlideSegment(new List<Location>());
						var slideText = text[start..(start + length - 1)];

						do
						{
							if (!parent.enumerator.MoveNext())
							{
								ErrorHandler.DeserializationError(token, "Unexpected end of file reached.");
								break;
							}

							var current = parent.enumerator.Current;
							if (Deserializer.TryReadLocation(current, out var location))
							{
								segment.vertices.Add(location);
							}
						} while (parent.enumerator.Current.type != TokenType.Location);

						segment.slideType = slideText switch
						                    {
							                    "-" => SlideType.StraightLine,
							                    ">" => Deserializer.DetermineRingType(currentNote.location, segment.vertices[0], 1),
							                    "<" => Deserializer.DetermineRingType(currentNote.location, segment.vertices[0], -1),
							                    "^" => Deserializer.DetermineRingType(currentNote.location, segment.vertices[0]),
							                    "qq" => SlideType.EdgeCurveCw,
							                    "q" => SlideType.CurveCw,
							                    "pp" => SlideType.EdgeCurveCcw,
							                    "p" => SlideType.CurveCcw,
							                    "v" => SlideType.Fold,
							                    "V" => SlideType.EdgeFold,
							                    "s" => SlideType.ZigZagS,
							                    "z" => SlideType.ZigZagZ,
							                    "w" => SlideType.Fan,
							                    _ => throw ErrorHandler.DeserializationError(token,
								                         "Slide type not recognized.")
						                    };
						break;
					}
					case TokenType.Duration:
						break;
					case TokenType.SlideJoiner:
						break;
					case TokenType.TimeStep:
						break;
					case TokenType.EachDivider:
						break;
					case TokenType.EndOfFile:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			parent.currentNoteCollection.AddNote(currentNote);
		}
	}
}