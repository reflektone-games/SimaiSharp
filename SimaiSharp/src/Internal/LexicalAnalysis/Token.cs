using System;

namespace SimaiSharp.Internal.LexicalAnalysis
{
	internal readonly struct Token
	{
		public readonly TokenType            type;
		public readonly ReadOnlyMemory<char> lexeme;
		public readonly int                  line;

		public Token(TokenType type, ReadOnlyMemory<char> lexeme, int line)
		{
			this.type   = type;
			this.lexeme = lexeme;
			this.line   = line;
		}
	}
}