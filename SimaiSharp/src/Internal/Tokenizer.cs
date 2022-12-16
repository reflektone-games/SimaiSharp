using System;
using System.Collections.Generic;

namespace SimaiSharp.Internal
{
	internal sealed class Tokenizer : SequenceAnalyzer<char>
	{
		public Tokenizer(string source) : base(source.ToCharArray())
		{
		}

		private readonly List<Token> _tokens = new();

		public List<Token> GetTokens()
		{
			var sourceSpan = new ReadOnlySpan<char>(sequence);

			while (!IsAtEnd && _tokens[^1].type != TokenType.EndOfFile)
			{
				start = current;
				ScanToken(in sourceSpan);
			}

			if (_tokens[^1].type != TokenType.EndOfFile)
				AddToken(in sourceSpan, TokenType.EndOfFile);

			return _tokens;
		}

		private void ScanToken(in ReadOnlySpan<char> sourceSpan)
		{
			item++;
			var c = Advance(sourceSpan);
			switch (c)
			{
				case ',':
					AddToken(in sourceSpan, TokenType.TimeStep);
					break;
				
				case '(':
					AddSectionDeclaration(in sourceSpan, TokenType.Tempo, ')');
					break;
				case '{':
					AddSectionDeclaration(in sourceSpan, TokenType.Subdivision, '}');
					break;
				case '[':
					AddSectionDeclaration(in sourceSpan, TokenType.Duration, ']');
					break;

				case var _ when IsReadingLocationDeclaration(in sourceSpan, out var length):
					current += length - 1;
					AddToken(in sourceSpan, TokenType.Location);
					break;
				
				case var _ when DecoratorChars.Contains(c):
					AddToken(in sourceSpan, TokenType.Decorator);
					break;
				
				case var _ when IsReadingSlideDeclaration(in sourceSpan, out var length):
					current += length - 1;
					AddToken(in sourceSpan, TokenType.Slide);
					break;
				
				case '*':
					AddToken(in sourceSpan, TokenType.SlideJoiner);
					break;
				
				case '/' or '\\':
					AddToken(in sourceSpan, TokenType.EachDivider);
					break;
				
				case ' ':
				case '\r':
				case '\t':
					// Ignore whitespace.
					break;

				case '\n':
					line++;
					item = 0;
					break;

				case 'E':
					AddToken(in sourceSpan, TokenType.EndOfFile);
					break;

				default:
					ErrorHandler.Report(line, item, c.ToString(), "Unexpected character.");
					break;
			}
		}

		private bool IsReadingLocationDeclaration(in ReadOnlySpan<char> sourceSpan, out int length)
		{
			var firstLocationChar = PeekPrevious(in sourceSpan);

			if (IsButtonLocation(firstLocationChar))
			{
				length = 1;
				return true;
			}

			if (IsSensorLocation(firstLocationChar))
			{
				var secondLocationChar = Peek(in sourceSpan);

				if (IsButtonLocation(secondLocationChar))
				{
					length = 2;
					return true;
				}

				if (firstLocationChar == 'C')
				{
					length = 1;
					return true;
				}

				ErrorHandler.Report(line, item, secondLocationChar.ToString(), "Invalid touch note expression.");
				length = 0;
				return false;
			}
			
			length = 0;
			return false;
		}

		private bool IsReadingSlideDeclaration(in ReadOnlySpan<char> sourceSpan, out int length)
		{
			if (!SlideChars.Contains(PeekPrevious(in sourceSpan)))
			{
				length = 0;
				return false;
			}

			var nextChar = Peek(in sourceSpan);

			length = nextChar is 'p' or 'q' ? 2 : 1;
			return true;
		}

		private void AddSectionDeclaration(in ReadOnlySpan<char> sourceSpan, TokenType tokenType, char terminator)
		{
			start++;
			while (Peek(in sourceSpan) != terminator)
			{
				if (IsAtEnd)
				{
					ErrorHandler.Report(line, item, sourceSpan[start..(current - 1)].ToString(), "Unterminated tempo.");
					return;
				}

				Advance(in sourceSpan);
			}

			AddToken(in sourceSpan, tokenType);
			
			// The terminator.
			Advance(in sourceSpan);
		}

		private void AddToken(in ReadOnlySpan<char> sourceSpan, TokenType type)
		{
			var text = sourceSpan[start..current].ToString();
			_tokens.Add(new Token(type, text, line));
		}

		private static bool IsSensorLocation(char value) => value is >= 'A' and <= 'E';
		private static bool IsButtonLocation(char value) => value is >= '0' and <= '9';

		private static readonly HashSet<char> DecoratorChars = new()
		                                                   {
			                                                   'f', 'b', 'x', 'h',
			                                                   '!', '?'
		                                                   };
		
		private static readonly HashSet<char> SlideChars = new()
		                                                   {
			                                                   '-',
			                                                   '>', '<', '^',
			                                                   'p', 'q',
			                                                   'v', 'V',
			                                                   's', 'z',
			                                                   'w',
		                                                   };
	}
}