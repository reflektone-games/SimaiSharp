using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

		public async Task<IEnumerable<KeyValuePair<string, string>>> ToKeyValuePairs()
		{
			var bytes = await File.ReadAllBytesAsync(_fullFilePath);
			bytes.TryDecodeText(out var rawData);

			var parameterPairs = Regex.Split(rawData, @"\n+(?=&)", RegexOptions.Multiline)
			                          .Select(Extensions.RemoveLineEndings);

			return parameterPairs
			       .Where(parameterPair => !string.IsNullOrEmpty(parameterPair) && parameterPair.Contains('='))
			       .Select(parameterPair => parameterPair.Split(new[] { '=' }, 2))
			       // skips the first character (&) in the key
			       .Select(parameterArray =>
				               new KeyValuePair<string, string>(parameterArray[0][1..], parameterArray[1]));
		}

		public async ValueTask<string> GetValue(string key)
		{
			return (await ToKeyValuePairs()).FirstOrDefault(parameterPair => parameterPair.Key == key).Value;
		}
	}
}