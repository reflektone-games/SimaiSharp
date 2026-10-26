using System;
using SimaiSharp.ErrorHandling;
using SimaiSharp.Utilities;

namespace SimaiSharp.Processors;

internal static class TempoProcessor
{
    public static void Deserialize(ReadOnlySpan<byte> bytes)
    {
        var (startLine, startColumn) = SimaiDeserializer.GetCurrentPosition();
        byte currentByte;

        var startInclusive = SimaiDeserializer.charIndex;
        do
        {
            currentByte = SimaiDeserializer.Pop(bytes);

            if (SimaiDeserializer.isEndOfFile)
                SimaiDeserializer.ThrowContext<ChartFormatException>();
        } while (currentByte != Constants.TempoBracketCloseChar);

        if (!bytes[startInclusive..(SimaiDeserializer.charIndex - 1)].TryParseFloat(out var result))
            SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

        SimaiDeserializer.tempo.time  = SimaiDeserializer.time;
        SimaiDeserializer.tempo.tempo = result;
    }

    internal static void ConsumeSubdivision(ReadOnlySpan<byte> bytes)
    {
        var (startLine, startColumn) = SimaiDeserializer.GetCurrentPosition();
        var startInclusive    = SimaiDeserializer.charIndex;
        var explicitTempoMode = false;

        // Consume the first char to see if we need to enable explicitTempoMode
        var currentByte = SimaiDeserializer.Pop(bytes);

        if (SimaiDeserializer.isEndOfFile)
            SimaiDeserializer.ThrowContext<ChartFormatException>();

        if (currentByte == Constants.HashChar)
        {
            explicitTempoMode = true;
            startInclusive++;
        }

        do
        {
            currentByte = SimaiDeserializer.Pop(bytes);

            if (SimaiDeserializer.isEndOfFile)
                SimaiDeserializer.ThrowContext<ChartFormatException>();
        } while (currentByte != Constants.SubdivisionCloseChar);

        if (!bytes[startInclusive..(SimaiDeserializer.charIndex - 1)].TryParseFloat(out var result))
            SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

        SimaiDeserializer.tempo.time = SimaiDeserializer.time;

        if (explicitTempoMode)
            SimaiDeserializer.tempo.SetSeconds(result);
        else
            SimaiDeserializer.tempo.subdivisions = result;
    }
}
