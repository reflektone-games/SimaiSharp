using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SimaiSharp.Internal;

namespace SimaiSharp
{
	public sealed class SimaiFile
	{
		private readonly string _fullFilePath;

		public SimaiFile(string path)
		{
			_fullFilePath = path;
		}

		public IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs()
		{
			const int sampleSize = 64;

			using var fileStream = new FileStream(_fullFilePath, FileMode.Open, FileAccess.Read);

			// Determine the encoding of the file
			var buffer       = new byte[64];
			var numCharsRead = fileStream.Read(buffer, 0, 64);
			var encoding     = buffer[..numCharsRead].TryGetEncoding(sampleSize);

			using var reader = new StreamReader(fileStream, encoding);
			// We've already read 64 chars in line 27, so we'll reset here.
			reader.BaseStream.Position = 0;

			var currentKey   = "";
			var currentValue = new StringBuilder();

			while (!reader.EndOfStream)
			{
				var line = reader.ReadLine();

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
	}
}