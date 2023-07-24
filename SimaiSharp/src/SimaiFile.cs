using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SimaiSharp.Internal;

namespace SimaiSharp
{
	public sealed class SimaiFile : IDisposable
	{
		private readonly StreamReader _simaiReader;

		public SimaiFile(FileSystemInfo file)
		{
			const int sampleSize = 64;

			var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);

			// Determine the encoding of the file
			var buffer       = new byte[64];
			var numCharsRead = fileStream.Read(buffer, 0, 64);
			var encoding     = buffer[..numCharsRead].TryGetEncoding(sampleSize);

			// We've already read 64 chars, so we'll reset here.
			fileStream.Position = 0;

			_simaiReader = new StreamReader(fileStream, encoding);
		}

		public SimaiFile(string text)
		{
			var stream = new MemoryStream();
			var writer = new StreamWriter(stream);
			writer.Write(text);
			writer.Flush();
			stream.Position = 0;

			_simaiReader = new StreamReader(stream);
		}

		public SimaiFile(Stream stream)
		{
			_simaiReader = new StreamReader(stream);
		}

		public SimaiFile(StreamReader reader)
		{
			_simaiReader = reader;
		}

		public IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs()
		{
			var currentKey   = string.Empty;
			var currentValue = new StringBuilder();

			while (!_simaiReader.EndOfStream)
			{
				var line = _simaiReader.ReadLine();

				if (line == null)
					break;

				if (line.StartsWith('&'))
				{
					if (currentKey != string.Empty)
					{
						yield return new KeyValuePair<string, string>(currentKey, currentValue.ToString().Trim());
						currentValue.Clear();
					}

					var keyValuePair = line.Split('=', 2);
					currentKey = keyValuePair[0][1..];
					currentValue.AppendLine(keyValuePair[1]);
				}
				else
				{
					currentValue.AppendLine(line);
				}
			}

			// Add the last entry
			yield return new KeyValuePair<string, string>(currentKey, currentValue.ToString().Trim());
		}

		public string GetValue(string key)
		{
			return ToKeyValuePairs().FirstOrDefault(parameterPair => parameterPair.Key == key).Value;
		}

		public void Dispose()
		{
			_simaiReader.Dispose();
		}
	}
}
