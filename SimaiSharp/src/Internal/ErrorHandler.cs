using System;

namespace SimaiSharp.Internal
{
	internal static class ErrorHandler
	{
		public static void TokenizationError(int line, int character, string location, string message)
		{
			throw new SimaiException(line, character, location, message);
		}

		public static void DeserializationError(Token token, string message)
		{
			throw new SimaiException($"{token} - {message}");
		}
	}
	
	[Serializable]
	internal class SimaiException : Exception
	{
		public SimaiException(int line, int character, string location, string message)
			: base($"Error: {message} at {location} ({line}, {character})")
		{ }
		
		public SimaiException(string message)
			: base($"Error: {message}")
		{ }
	}
}