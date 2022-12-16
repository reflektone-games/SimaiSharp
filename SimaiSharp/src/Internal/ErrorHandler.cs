using System;

namespace SimaiSharp.Internal
{
	internal static class ErrorHandler
	{
		public static void Report(int line, int character, string location, string message)
		{
			throw new SimaiException(line, character, location, message);
		}
	}
	
	[Serializable]
	internal class SimaiException : Exception
	{
		public SimaiException(int line, int character, string location, string message)
			: base($"Error: {message} at {location} ({line}, {character})")
		{ }
	}
}