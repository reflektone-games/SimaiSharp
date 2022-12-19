using System;
using System.Collections.Generic;
using SimaiSharp.Internal.LexicalAnalysis;
using SimaiSharp.Structures;

namespace SimaiSharp.Internal.SyntacticAnalysis.States
{
	internal static class NoteReader
	{
		public static Note Process(Deserializer parent, Token identityToken)
		{
			if (!Deserializer.TryReadLocation(in identityToken, out var noteLocation))
				throw ErrorHandler.DeserializationError(identityToken, $"Invalid location declaration.");

			var currentNote = new Note
			                  {
				                  location = noteLocation
			                  };

			if (noteLocation.group != NoteGroup.Tap)
				currentNote.type = NoteType.Touch;
			
			// Some readers (e.g. NoteReader) moves the enumerator automatically.
			// We can skip moving the pointer if that's satisfied.
			var manuallyMoved = false;

			while (manuallyMoved || parent.enumerator.MoveNext())
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
						DecorateNote(in token, ref currentNote);
						break;
					}
					case TokenType.Slide:
					{
						var slide   = SlideReader.Process(parent, in currentNote, in token);
						manuallyMoved = true;
						
						currentNote.slidePaths ??= new List<SlidePath>();
						currentNote.slidePaths.Add(slide);
						break;
					}
					case TokenType.Duration:
					{
						ReadDuration(in token, in parent.currentTiming, ref currentNote);
						break;
					}
					case TokenType.SlideJoiner:
						throw ErrorHandler.DeserializationError(in token,
						                                        "Slide joiners should be declared in slide scopes.");
					case TokenType.TimeStep:
					case TokenType.EachDivider:
					case TokenType.EndOfFile:
					case TokenType.Location:
						// note terminates here
						return currentNote;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return currentNote;
		}

		private static void DecorateNote(in Token token, ref Note note)
		{
			switch (token.lexeme.Span[0])
			{
				case 'b':
					// always override note type
					note.type = NoteType.Break;
					return;
				case 'h' when note.type != NoteType.Break:
					note.type   =   NoteType.Hold;
					note.length ??= 0;
					return;
				case 'x':
					note.styles |= NoteStyle.Ex;
					return;
				case 'f':
					note.styles |= NoteStyle.Fireworks;
					return;
			}
		}

		private static void ReadDuration(in Token token, in TimingChange timing, ref Note note)
		{
			if (token.lexeme.Span[0] == '#')
			{
				if (!float.TryParse(token.lexeme.Span[1..], out var explicitValue))
					throw ErrorHandler.DeserializationError(token, "Invalid explicit hold duration syntax.");

				note.length = explicitValue;
				return;
			}

			var indexOfSeparator = token.lexeme.Span.IndexOf(':');
			if (!float.TryParse(token.lexeme.Span[..indexOfSeparator], out var nominator))
				throw ErrorHandler.DeserializationError(token, "Invalid hold duration nominator.");

			if (!float.TryParse(token.lexeme.Span[(indexOfSeparator + 1)..], out var denominator))
				throw ErrorHandler.DeserializationError(token, "Invalid hold duration denominator.");

			note.length = timing.SecondsPerBar / (nominator / 4) * denominator;
		}
	}
}