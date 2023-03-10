using System;
using SimaiSharp.Internal.LexicalAnalysis;

namespace SimaiSharp.Internal
{
	internal static class ErrorHandler
	{
		public static Exception TokenizationError(int line, int character, string location, string message)
		{
			return new SimaiException(line, character, location, message);
		}

		public static Exception DeserializationError(in Token token, string message)
		{
			return new SimaiException(token, message);
		}
	}

	[Serializable]
	internal class SimaiException : Exception
	{
		public SimaiException(int line, int character, string location, string message)
			: base($"{message}: {location} (on line {line}, char {character})")
		{
		}

		public SimaiException(Token token, string message)
			: base($"{message}. {token.type} {token.lexeme} (on line {token.line})")
		{
		}
	}
}