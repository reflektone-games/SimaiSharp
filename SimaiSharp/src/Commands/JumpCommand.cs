using System;
using SimaiSharp.Utilities;

namespace SimaiSharp.Commands;

public sealed class JumpCommand : Command
{
    public override void Deserialize(ReadOnlySpan<byte> chartSpan)
    {
        var key = Hashing.ComputeHash(chartSpan);

        if (SimaiDeserializer.JumpMarkers.TryGetValue(key, out var time))
            SimaiDeserializer.time = time;
        else
            SimaiDeserializer.JumpMarkers[key] = SimaiDeserializer.time;
    }
}
