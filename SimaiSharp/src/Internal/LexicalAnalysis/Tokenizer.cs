using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SimaiSharp.Internal.LexicalAnalysis
{
	internal sealed class Tokenizer
	{
		private const char Space            = (char)0x0020;
		private const char EnSpace          = (char)0x2002;
		private const char PunctuationSpace = (char)0x2008;
		private const char IdeographicSpace = (char)0x3000;

		private const char LineSeparator      = (char)0x2028;
		private const char ParagraphSeparator = (char)0x2029;

		private static readonly HashSet<char> EachDividerChars = new()
		                                                         {
			                                                         '/', '`'
		                                                         };

		private static readonly HashSet<char> DecoratorChars = new()
		                                                       {
			                                                       'f', 'b', 'x', 'h',
			                                                       '!', '?',
			                                                       '@', '$'
		                                                       };

		private static readonly HashSet<char> SlideChars = new()
		                                                   {
			                                                   '-',
			                                                   '>', '<', '^',
			                                                   'p', 'q',
			                                                   'v', 'V',
			                                                   's', 'z',
			                                                   'w'
		                                                   };

		private static readonly HashSet<char> SeparatorChars = new()
		                                                       {
			                                                       '\r',
			                                                       '\t',
			                                                       LineSeparator,
			                                                       ParagraphSeparator,
			                                                       Space,
			                                                       EnSpace,
			                                                       PunctuationSpace,
			                                                       IdeographicSpace
		                                                       };

		private readonly ReadOnlyMemory<char> _sequence;
		private          int                  _current;
		private          int                  _item;
		private          int                  _line = 1;

		private int _start;

		public Tokenizer(string sequence)
		{
			_sequence = sequence.AsMemory();
		}

		private bool IsAtEnd => _current >= _sequence.Length;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerable<Token> GetTokens()
		{
			while (!IsAtEnd)
			{
				_start = _current;

				var nextToken = ScanToken();
				if (nextToken.HasValue)
					yield return nextToken.Value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Token? ScanToken()
		{
			_item++;
			var c = Advance();
			switch (c)
			{
				case ',':
					return CompileToken(TokenType.TimeStep);

				case '(':
					return CompileSectionDeclaration(TokenType.Tempo, ')');
				case '{':
					return CompileSectionDeclaration(TokenType.Subdivision, '}');
				case '[':
					return CompileSectionDeclaration(TokenType.Duration, ']');

				case var _ when IsReadingLocationDeclaration(out var length):
					_current += length - 1;
					return CompileToken(TokenType.Location);

				case var _ when DecoratorChars.Contains(c):
					return CompileToken(TokenType.Decorator);

				case var _ when IsReadingSlideDeclaration(out var length):
					_current += length - 1;
					return CompileToken(TokenType.Slide);

				case '*':
					return CompileToken(TokenType.SlideJoiner);

				case var _ when EachDividerChars.Contains(c):
					return CompileToken(TokenType.EachDivider);

				case var _ when SeparatorChars.Contains(c):
					// Ignore whitespace.
					return null;

				case '\n':
					_line++;
					_item = 0;
					return null;

				case 'E':
					return CompileToken(TokenType.EndOfFile);

				case '|':
				{
					if (Peek() != '|')
						throw ErrorHandler.TokenizationError(_line, _item, Peek().ToString(),
						                                     "Unexpected character");

					while (Peek() != '\n')
						Advance();

					return null;
				}

				default:
					throw ErrorHandler.TokenizationError(_line, _item, c.ToString(), "Unexpected character");
			}
		}

		private bool IsReadingLocationDeclaration(out int length)
		{
			var firstLocationChar = PeekPrevious();

			if (IsButtonLocation(firstLocationChar))
			{
				length = 1;
				return true;
			}

			length = 0;

			if (!IsSensorLocation(firstLocationChar)) return false;

			var secondLocationChar = Peek();

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

			var secondCharIsEmpty = SeparatorChars.Contains(secondLocationChar) ||
			                        secondLocationChar is '\n' or '\0';

			// This is the notation for EOF.
			if (firstLocationChar == 'E' && secondCharIsEmpty)
				return false;

			throw ErrorHandler.TokenizationError(_line, _item, secondLocationChar.ToString(),
			                                     "Invalid touch note expression");
		}

		private bool IsReadingSlideDeclaration(out int length)
		{
			if (!SlideChars.Contains(PeekPrevious()))
			{
				length = 0;
				return false;
			}

			var nextChar = Peek();

			length = nextChar is 'p' or 'q' ? 2 : 1;
			return true;
		}

		private Token? CompileSectionDeclaration(TokenType tokenType, char terminator)
		{
			_start++;
			while (Peek() != terminator)
			{
				if (IsAtEnd)
					throw ErrorHandler.TokenizationError(_line, _item,
					                                     _sequence.Span[(_start - 1).._start].ToString(),
					                                     "Unterminated section. " +
					                                     $"You probably want to add a {terminator} here");

				Advance();
			}

			var token = CompileToken(tokenType);

			// The terminator.
			Advance();

			return token;
		}

		private Token CompileToken(TokenType type)
		{
			var text = _sequence[_start.._current];
			return new Token(type, text, _line);
		}

		private static bool IsSensorLocation(char value)
		{
			return value is >= 'A' and <= 'E';
		}

		private static bool IsButtonLocation(char value)
		{
			return value is >= '0' and <= '8';
		}

		/// <summary>
		///     Returns the <see cref="_current" /> glyph, and increments by one.
		/// </summary>
		private char Advance()
		{
			return _sequence.Span[_current++];
		}

		/// <summary>
		///     Returns the <see cref="_current" /> glyph without incrementing.
		/// </summary>
		private char Peek()
		{
			return IsAtEnd ? default : _sequence.Span[_current];
		}

		/// <summary>
		///     Returns the last glyph without decrementing.
		/// </summary>
		private char PeekPrevious()
		{
			return _current == 0 ? default : _sequence.Span[_current - 1];
		}
	}
}