using System;
using System.Collections.Generic;
using SimaiSharp.Internal.LexicalAnalysis;
using SimaiSharp.Structures;

namespace SimaiSharp.Internal.SyntacticAnalysis.States
{
	internal static class SlideReader
	{
		public static SlidePath Process(Deserializer parent,
		                                in Note      currentNote,
		                                in Token     identityToken)
		{
			var path    = new SlidePath();
			var segment = new SlideSegment(new List<Location>(1));

			var length = identityToken.lexeme.Length;
			segment.slideType = IdentifySlideType(currentNote, in identityToken, in segment, in length);
			AssignVertices(parent, identityToken, ref segment);

			do
			{
				var token = parent.enumerator.Current;

				switch (token.type)
				{
					case TokenType.Tempo:
						throw ErrorHandler.DeserializationError(in token,
						                                        "Tempo should be declared outside of note scope.");
					case TokenType.Subdivision:
						throw ErrorHandler.DeserializationError(in token,
						                                        "Subdivision should be declared outside of note scope.");
					case TokenType.Decorator:
					{
						DecorateSlide(in token, ref segment);
						break;
					}
					case TokenType.Slide:
					{
						var slide = SlideReader.Process(parent, in currentNote, in token);
						path.segments.Add(slide);
						break;
					}
					case TokenType.Duration:
					{
						ReadDuration(in token, in parent.currentTiming, ref segment);
						break;
					}
					case TokenType.SlideJoiner:
					case TokenType.TimeStep:
					case TokenType.EachDivider:
					case TokenType.EndOfFile:
					case TokenType.Location:
						// slide terminates here
						return path;
					default:
						throw new ArgumentOutOfRangeException();
				}
			} while (parent.enumerator.MoveNext());

			return path;
		}

		private static void DecorateSlide(in Token token, ref SlideSegment segment)
		{
			switch (token.lexeme.Span[0])
			{
				case 'b':
					segment.isBreak = true;
					return;
				default:
					throw ErrorHandler.DeserializationError(token, "Invalid slide decorator.");
			}
		}

		private static SlideType IdentifySlideType(Note            currentNote,
		                                           in Token        identityToken,
		                                           in SlideSegment segment,
		                                           in int          length)
		{
			return identityToken.lexeme.Span[0] switch
			       {
				       '-' => SlideType.StraightLine,
				       '>' => Deserializer.DetermineRingType(currentNote.location,
				                                             segment.vertices[0],
				                                             1),
				       '<' => Deserializer.DetermineRingType(currentNote.location,
				                                             segment.vertices[0],
				                                             -1),
				       '^' => Deserializer.DetermineRingType(currentNote.location,
				                                             segment.vertices[0]),
				       'q' when length == 2 && identityToken.lexeme.Span[1] == 'q' =>
					       SlideType.EdgeCurveCw,
				       'q' => SlideType.CurveCw,
				       'p' when length == 2 && identityToken.lexeme.Span[1] == 'p' =>
					       SlideType.EdgeCurveCcw,
				       'p' => SlideType.CurveCcw,
				       'v' => SlideType.Fold,
				       'V' => SlideType.EdgeFold,
				       's' => SlideType.ZigZagS,
				       'z' => SlideType.ZigZagZ,
				       'w' => SlideType.Fan,
				       _ => throw ErrorHandler.DeserializationError(in identityToken,
				                                                    "Slide type not recognized.")
			       };
		}

		private static void AssignVertices(Deserializer parent, in Token identityToken, ref SlideSegment segment)
		{
			do
			{
				if (!parent.enumerator.MoveNext())
					throw ErrorHandler.DeserializationError(in identityToken,
					                                        "Unexpected end of file reached.");

				var current = parent.enumerator.Current;
				if (Deserializer.TryReadLocation(in current,
				                                 out var location))
				{
					segment.vertices.Add(location);
				}
			} while (parent.enumerator.Current.type != TokenType.Location);
		}
		
		private static void ReadDuration(in Token token, in TimingChange timing, ref Note note)
		{
			// TODO: Implement slide duration readout
			// REFERENCE: https://w.atwiki.jp/simai/pages/25.html#id_3afb985d
			
			// Slide duration:
			// By beat: X:Y (subdivisions)
			// By explicit statement: D (seconds)
			
			// (Optional) Intro delay duration:
			// By tempo: T#d (T: tempo, d: slide duration)
			// By explicit statement: D##d (D: seconds, d: slide duration)
		}
	}
}