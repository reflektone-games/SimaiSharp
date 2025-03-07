using System;
using System.IO;
using SimaiSharp.Structures;

namespace SimaiSharp
{
    public static class SimaiConvert
    {
        public static SimaiChart Deserialize(Span<byte> bytes) => SimaiDeserializer.Deserialize(bytes);

        public static StreamWriter Serialize(SimaiChart chart)
        {
            throw new NotImplementedException();
        }
    }
}
