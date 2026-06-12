using System.Collections.Generic;
using System.IO;

namespace SimaiSharp.FileWriting;

public class SimaiFileWriter(Dictionary<string, string>? mappings = null)
{
    private readonly Dictionary<string, string> _mappings = mappings ?? new Dictionary<string, string>();

    public void AddParameter(string    key, string     value) => _mappings.Add(key, value);
    public bool TryGetValue(string     key, out string value) => _mappings.TryGetValue(key, out value);
    public bool RemoveParameter(string key) => _mappings.Remove(key);

    public void WriteToStream(Stream stream)
    {
        using var streamWriter = new StreamWriter(stream);
        foreach (var (key, value) in _mappings)
        {
            streamWriter.WriteLine($"&{key}={value}");
        }
    }
}
