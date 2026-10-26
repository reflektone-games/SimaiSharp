using System;
using SimaiSharp.ErrorHandling;

namespace SimaiSharp.Processors;

public static class CommandProcessor
{
    public static void Deserialize(ReadOnlySpan<byte> bytes)
    {
        var (startLine, startColumn) = SimaiDeserializer.GetCurrentPosition();
        byte currentByte;

        var startInclusive = SimaiDeserializer.charIndex;
        var separatorIndex = -1;
        do
        {
            currentByte = SimaiDeserializer.Pop(bytes);

            if (currentByte is Constants.NewSlideOrCommandArgumentChar or Constants.HashChar)
                separatorIndex = SimaiDeserializer.charIndex - 1;

            if (SimaiDeserializer.isEndOfFile)
                SimaiDeserializer.ThrowContext<ChartFormatException>(startLine, startColumn);
        } while (currentByte != Constants.RingRightOrCommandCloseChar);

        if (separatorIndex == -1)
            SimaiDeserializer.ThrowContext<ChartFormatException>(startLine, startColumn);

        if (!SimaiConvert.TryGetCommand(bytes[startInclusive..separatorIndex], out var command))
            SimaiDeserializer.ThrowContext<ChartFormatException>(startLine, startColumn);

        try
        {
            command.Deserialize(bytes[(separatorIndex + 1)..(SimaiDeserializer.charIndex - 1)]);
        }
        catch (Exception)
        {
            SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);
        }
    }
}
