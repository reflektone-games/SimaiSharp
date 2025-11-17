using System;
using System.IO;
using SimaiSharp.Structures;

namespace SimaiSharp
{
    public static class SimaiConvert
    {
        public static SimaiChart Deserialize(ReadOnlySpan<byte> bytes) => SimaiDeserializer.Deserialize(bytes);
        public static void       Serialize(SimaiChart chart, StreamWriter writer) => SimaiSerializer.Serialize(chart, writer);
    }
}
