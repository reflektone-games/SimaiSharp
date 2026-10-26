using System;
using System.Runtime.CompilerServices;
using SimaiSharp.ErrorHandling;
using SimaiSharp.Structures;
using SimaiSharp.Utilities;

namespace SimaiSharp.Processors;

public static class SlideProcessor
{
    public static void Deserialize(ReadOnlySpan<byte> bytes, byte currentByte, SlidePath slidePath)
    {
        var secondByte = SimaiDeserializer.Pop(bytes);
        var targetLocation =
            NoteProcessor.ConsumeLocation(
                bytes,
                secondByte is Constants.CurveCwChar or Constants.CurveCcwChar ? SimaiDeserializer.Pop(bytes) : secondByte);

        if (SimaiDeserializer.isEndOfFile)
            SimaiDeserializer.ThrowContext<ChartFormatException>();

        var slideType = currentByte switch
        {
            Constants.StraightLineChar => SlideType.StraightLine,
            Constants.RingRightOrCommandCloseChar => FromRingRight(slidePath.vertices[^1] & 0b111),
            Constants.RingLeftOrCommandOpenChar => FromRingLeft(slidePath.vertices[^1] & 0b111),
            Constants.RingAutoShortChar => FromRingShortest(slidePath.vertices[^1], targetLocation),
            Constants.CurveCwChar when secondByte is Constants.CurveCwChar => SlideType.EdgeCurveCw,
            Constants.CurveCwChar => SlideType.CurveCw,
            Constants.CurveCcwChar when secondByte is Constants.CurveCcwChar => SlideType.EdgeCurveCcw,
            Constants.CurveCcwChar => SlideType.CurveCcw,
            Constants.FoldChar => SlideType.Fold,
            Constants.EdgeFoldChar => SlideType.EdgeFold,
            Constants.ZigZagSChar => SlideType.ZigZagS,
            Constants.ZigZagZChar => SlideType.ZigZagZ,
            Constants.FanChar => SlideType.Fan,
            _ => SlideType.StraightLine
        };

        slidePath.segments.Add(new SlideSegment
        {
            type       = slideType,
            startIndex = slidePath.vertices.Count - 1
        });

        slidePath.vertices.Add(targetLocation);

        if (slideType != SlideType.EdgeFold)
            return;

        targetLocation = NoteProcessor.ConsumeLocation(bytes, SimaiDeserializer.Pop(bytes));

        if (SimaiDeserializer.isEndOfFile)
            SimaiDeserializer.ThrowContext<ChartFormatException>();

        slidePath.vertices.Add(targetLocation);
    }

    private static SlideType FromRingRight(int startLocation) =>
        (SlideType)(IsBottomHalf(startLocation) + 1);

    private static SlideType FromRingLeft(int startLocation) =>
        (SlideType)(2 - IsBottomHalf(startLocation));

    private static SlideType FromRingShortest(int startLocation, int endLocation)
    {
        var difference = endLocation.ToNoteIndex() - startLocation.ToNoteIndex();

        // Masking 0b111 makes negative values 8 - value.
        // (Transforming the difference to be the clockwise distance)
        // If the clockwise path is longer than half a ring (>=4), the sign bit will be 1
        var lesserThanHalfRingBit = ((difference & 0b0111) - 4) >> 31;

        // The bit manipulation calculates the CW path.
        // If it's smaller or equal to 4, it's CW (-1), otherwise it's CCW (0)
        return (SlideType)(2 + lesserThanHalfRingBit);
    }

    /// XOR Bit 1 and Bit 2 (0b010 and 0b100), this means the start location is in the bottom four buttons
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IsBottomHalf(int buttonIndex) => (buttonIndex >> 2) ^ ((buttonIndex >> 1) & 1);

    /// <summary>
    /// https://w.atwiki.jp/simai/pages/25.html#id_3afb985d
    /// </summary>
    internal static void ConsumeSlideDuration(ReadOnlySpan<byte> bytes, ref SlidePath slidePath)
    {
        var (startLine, startColumn) = SimaiDeserializer.GetCurrentPosition();

        byte currentByte;
        var  startInclusive = SimaiDeserializer.charIndex;
        var  hashIndex      = 0;
        var  hashCount      = 0;
        var  colonIndex     = -1;
        var  localTempo     = SimaiDeserializer.tempo;

        do
        {
            currentByte = SimaiDeserializer.Pop(bytes);

            if (SimaiDeserializer.isEndOfFile)
                SimaiDeserializer.ThrowContext<ChartFormatException>(startLine, startColumn);

            switch (currentByte)
            {
                case Constants.HashChar:
                    if (hashCount == 0)
                        hashIndex = SimaiDeserializer.charIndex - 1;
                    hashCount++;
                    break;
                case Constants.ColonChar:
                    colonIndex = SimaiDeserializer.charIndex - 1;
                    break;
            }
        } while (currentByte != Constants.DurationCloseChar);

        switch (hashCount)
        {
            // [3##1.5]
            case 2:
            {
                if (hashIndex == startInclusive)
                    slidePath.delay = localTempo.SecondsPerBar;
                else if (bytes[startInclusive..hashIndex].TryParseFloat(out var delay))
                    slidePath.delay = delay;
                else
                    SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

                if (!bytes[(hashIndex + hashCount)..(SimaiDeserializer.charIndex - 1)].TryParseFloat(out var duration))
                    SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

                slidePath.duration += duration;
                break;
            }
            // [160#2]
            case 1 when colonIndex == -1:
            {
                if (hashIndex == startInclusive)
                    slidePath.delay = localTempo.SecondsPerBar;
                else if (bytes[startInclusive..hashIndex].TryParseFloat(out var newTempo))
                {
                    localTempo.tempo = newTempo;
                    slidePath.delay  = localTempo.SecondsPerBar;
                }
                else
                    SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

                if (!bytes[(hashIndex + 1)..].TryParseFloat(out var duration))
                    SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

                slidePath.duration += duration;
                break;
            }
            // [160#8:3]
            case 1:
            {
                if (hashIndex == startInclusive)
                    slidePath.delay = localTempo.SecondsPerBar;
                else if (bytes[startInclusive..hashIndex].TryParseFloat(out var newTempo))
                {
                    localTempo.tempo = newTempo;
                    slidePath.delay  = localTempo.SecondsPerBar;
                }
                else
                    SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

                if (!bytes[(hashIndex + hashCount)..colonIndex].TryParseFloat(out var nominator))
                    SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

                if (!bytes[(colonIndex + 1)..(SimaiDeserializer.charIndex - 1)].TryParseFloat(out var denominator))
                    SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

                slidePath.duration += localTempo.SecondsPerBar / (nominator / 4) * denominator;
                break;
            }
            // [8:3]
            case 0 when colonIndex != -1:
            {
                if (!(bytes[startInclusive..colonIndex]).TryParseFloat(out var nominator))
                    SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

                if (!(bytes[(colonIndex + 1)..(SimaiDeserializer.charIndex - 1)]).TryParseFloat(out var denominator))
                    SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

                slidePath.delay    =  localTempo.SecondsPerBar;
                slidePath.duration += localTempo.SecondsPerBar / (nominator / 4) * denominator;
                break;
            }
            default:
            {
                if (colonIndex == -1)
                    SimaiDeserializer.ThrowContext<ChartFormatException>(startLine, startColumn);
                break;
            }
        }
    }
}
