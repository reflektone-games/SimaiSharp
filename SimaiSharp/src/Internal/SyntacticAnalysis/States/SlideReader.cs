using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using SimaiSharp.Internal.LexicalAnalysis;
using SimaiSharp.Structures;

namespace SimaiSharp.Internal.SyntacticAnalysis.States
{
	internal static class SlideReader
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SlidePath Process(Deserializer parent,
		                                in Note      currentNote,
		                                in Token     identityToken)
		{
			var overrideTiming = new TimingChange
			                     {
				                     tempo = parent.currentTiming.tempo
			                     };

			var path = new SlidePath(new List<SlideSegment>())
			           {
				           delay = overrideTiming.SecondsPerBeat
			           };

			ReadSegment(parent, identityToken, currentNote.location, ref path);

			// Some readers (e.g. NoteReader) moves the enumerator automatically.
			// We can skip moving the pointer if that's satisfied.
			var manuallyMoved = true;

			while (!parent.endOfFile && (manuallyMoved || parent.MoveNext()))
			{
				var token = parent.enumerator.Current;
				manuallyMoved = false;

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
						DecorateSlide(in token, ref path);
						break;
					}
					case TokenType.Slide:
					{
						ReadSegment(parent, token, path.segments[^1].vertices[^1], ref path);
						manuallyMoved = true;
						break;
					}
					case TokenType.Duration:
					{
						ReadDuration(in token, in parent.currentTiming, ref path);
						break;
					}
					case TokenType.SlideJoiner:
					{
						parent.MoveNext();
						return path;
					}
					case TokenType.TimeStep:
					case TokenType.EachDivider:
					case TokenType.EndOfFile:
					case TokenType.Location:
						// slide terminates here
						return path;
					case TokenType.None:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return path;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ReadSegment(Deserializer  parent,
		                                Token         identityToken,
		                                Location      startingLocation,
		                                ref SlidePath path)
		{
			var segment = new SlideSegment(new List<Location>(1));
			var length  = identityToken.lexeme.Length;
			AssignVertices(parent, identityToken, ref segment);
			segment.slideType = IdentifySlideType(in identityToken, startingLocation, in segment, in length);

			path.segments.Add(segment);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void DecorateSlide(in Token token, ref SlidePath path)
		{
			switch (token.lexeme.Span[0])
			{
				case 'b':
					path.type = NoteType.Break;
					return;
				default:
					throw ErrorHandler.DeserializationError(token, "Invalid slide decorator.");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static SlideType IdentifySlideType(in Token        identityToken,
		                                           in Location     startingLocation,
		                                           in SlideSegment segment,
		                                           in int          length)
		{
			return identityToken.lexeme.Span[0] switch
			{
				'-' => SlideType.StraightLine,
				'>' => Deserializer.DetermineRingType(startingLocation,
				                                      segment.vertices[0],
				                                      1),
				'<' => Deserializer.DetermineRingType(startingLocation,
				                                      segment.vertices[0],
				                                      -1),
				'^' => Deserializer.DetermineRingType(startingLocation,
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
					segment.vertices.Add(location);
			} while (parent.enumerator.Current.type == TokenType.Location);
		}

		// REFERENCE: https://w.atwiki.jp/simai/pages/25.html#id_3afb985d
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ReadDuration(in Token token, in TimingChange timing, ref SlidePath path)
		{
			var startOfDurationDeclaration = 0;
			var overrideTiming             = timing;

			// (Optional) Intro delay duration:
			// By tempo: T#d (T: tempo, d: slide duration)
			// By explicit statement: D##d (D: seconds, d: slide duration)
			var firstHashIndex           = token.lexeme.Span.IndexOf('#');
			var statesIntroDelayDuration = firstHashIndex > -1;
			if (statesIntroDelayDuration)
			{
				startOfDurationDeclaration = token.lexeme.Span.LastIndexOf('#') + 1;
				var lastHashIndex = startOfDurationDeclaration - 1;

				var delayDeclaration    = token.lexeme.Span[..firstHashIndex];
				var isExplicitStatement = firstHashIndex != lastHashIndex;
				if (isExplicitStatement)
				{
					if (!float.TryParse(delayDeclaration,
					                    NumberStyles.Any,
					                    CultureInfo.InvariantCulture,
					                    out var explicitValue))
						throw ErrorHandler.DeserializationError(token, "Invalid explicit slide delay syntax.");

					path.delay = explicitValue;
				}
				else
				{
					if (!float.TryParse(delayDeclaration,
					                    NumberStyles.Any,
					                    CultureInfo.InvariantCulture,
					                    out var tempoValue))
						throw ErrorHandler.DeserializationError(token, "Invalid explicit slide delay syntax.");

					overrideTiming.tempo = tempoValue;
					path.delay           = overrideTiming.SecondsPerBar;
				}
			}

			var durationDeclaration = token.lexeme.Span[startOfDurationDeclaration..];
			var indexOfSeparator    = durationDeclaration.IndexOf(':');

			// Slide duration:
			// By beat: X:Y (subdivisions)
			// By explicit statement: D (seconds)
			if (indexOfSeparator == -1)
			{
				if (!float.TryParse(durationDeclaration,
				                    NumberStyles.Any,
				                    CultureInfo.InvariantCulture,
				                    out var explicitValue))
					throw ErrorHandler.DeserializationError(token, "Invalid explicit slide duration syntax.");

				path.duration = explicitValue;
				return;
			}

			if (!float.TryParse(durationDeclaration[..indexOfSeparator], NumberStyles.Any,
			                    CultureInfo.InvariantCulture,
			                    out var nominator))
				throw ErrorHandler.DeserializationError(token, "Invalid slide duration nominator.");

			if (!float.TryParse(durationDeclaration[(indexOfSeparator + 1)..],
			                    NumberStyles.Any,
			                    CultureInfo.InvariantCulture,
			                    out var denominator))
				throw ErrorHandler.DeserializationError(token, "Invalid slide duration denominator.");

			path.duration = overrideTiming.SecondsPerBar / (nominator / 4) * denominator;
		}
	}
}