using System;
using System.Runtime.CompilerServices;
using SimaiSharp.ErrorHandling;
using SimaiSharp.Structures;
using SimaiSharp.Utilities;

namespace SimaiSharp.Processors;

public static class NoteProcessor
{
    public static void Deserialize(ReadOnlySpan<byte> bytes, byte currentByte)
    {
        var noteLocation = ConsumeLocationDirect(bytes, currentByte);

        if (SimaiDeserializer.isEndOfFile)
            SimaiDeserializer.ThrowContext<ChartFormatException>();

        var noteExists            = true;
        var forceTapStar          = false;
        var noSlideIntroAnimation = false;

        SimaiDeserializer.note.location = noteLocation;
        SimaiDeserializer.note.category = noteLocation.ToNoteGroup() == 0 ? NoteCategory.Tap : NoteCategory.Touch;

        SlidePath? slidePath = null;

        while (true)
        {
            currentByte = SimaiDeserializer.Pop(bytes);

            switch (currentByte)
            {
                #region Decorators

                case Constants.FireworkChar:
                    SimaiDeserializer.note.styles |= NoteStyles.Fireworks;
                    break;
                case Constants.BreakChar:
                    if (slidePath != null)
                        slidePath.isBreak = true;
                    else
                        SimaiDeserializer.note.category = NoteCategory.Break;
                    break;
                case Constants.ExChar:
                    SimaiDeserializer.note.styles |= NoteStyles.Ex;
                    break;
                case Constants.MineChar:
                    if (slidePath != null)
                        slidePath.isMine = true;
                    else
                        SimaiDeserializer.note.styles |= NoteStyles.Mine;
                    break;
                case Constants.HoldChar:
                    if (SimaiDeserializer.note.category != NoteCategory.Break)
                        SimaiDeserializer.note.category = NoteCategory.Hold;
                    SimaiDeserializer.note.styles |= NoteStyles.Hold;
                    break;
                case Constants.TapRemovedSlideChar:
                    noteExists = false;
                    break;
                case Constants.SuddenSlideChar:
                    noteExists            = false;
                    noSlideIntroAnimation = true;

                    if (slidePath != null && slidePath.segments.Count != 0)
                        slidePath.noIntroAnimation = true;
                    break;
                case Constants.ForceNonStarChar:
                    forceTapStar = true;
                    break;
                case Constants.ForceStarChar:
                    SimaiDeserializer.note.styles |= NoteStyles.Star;
                    if (SimaiDeserializer.Peek(bytes) == Constants.ForceStarChar)
                        SimaiDeserializer.note.styles |= NoteStyles.Spinning;
                    break;

                #endregion

                case Constants.DurationOpenChar:
                    if (slidePath is not null)
                    {
                        SlideProcessor.ConsumeSlideDuration(bytes, ref slidePath);
                        SimaiDeserializer.chart.finishTiming =
                            Math.Max(SimaiDeserializer.time + slidePath.duration, SimaiDeserializer.chart.finishTiming);
                    }
                    else
                    {
                        ConsumeNoteDuration(bytes);
                        SimaiDeserializer.chart.finishTiming =
                            Math.Max(SimaiDeserializer.time + SimaiDeserializer.note.length,
                                     SimaiDeserializer.chart.finishTiming);
                    }

                    break;

                case Constants.NewSlideOrCommandArgumentChar:
                    if (slidePath != null && slidePath.segments.Count != 0)
                        SimaiDeserializer.noteGroup.slidePaths.Insert(SimaiDeserializer.slideIndex++, slidePath);
                    slidePath = CreateNewSlidePath(noSlideIntroAnimation);
                    break;

                case Constants.StraightLineChar:
                case Constants.RingRightOrCommandCloseChar:
                case Constants.RingLeftOrCommandOpenChar:
                case Constants.RingAutoShortChar:
                case Constants.CurveCwChar:
                case Constants.CurveCcwChar:
                case Constants.FoldChar:
                case Constants.EdgeFoldChar:
                case Constants.ZigZagSChar:
                case Constants.ZigZagZChar:
                case Constants.FanChar:
                    SimaiDeserializer.note.styles |=  NoteStyles.Star;
                    slidePath                     ??= CreateNewSlidePath(noSlideIntroAnimation);
                    SlideProcessor.Deserialize(bytes, currentByte, slidePath);
                    break;

                default:
                    // Resolve all pending data
                    if (slidePath != null && slidePath.segments.Count != 0)
                        SimaiDeserializer.noteGroup.slidePaths.Insert(SimaiDeserializer.slideIndex++, slidePath);

                    SimaiDeserializer.charIndex--;
                    goto FINALIZE;
            }
        }

    FINALIZE:
        if (!noteExists)
            return;

        if (forceTapStar)
            SimaiDeserializer.note.styles &= ~NoteStyles.Star;

        if ((SimaiDeserializer.note.styles & NoteStyles.Hold) != 0 && slidePath is { segments.Count: > 0 } &&
            SimaiDeserializer.note.length                     == 0)
            SimaiDeserializer.note.length = slidePath.delay;

        SimaiDeserializer.note.time = SimaiDeserializer.time;
        SimaiDeserializer.noteGroup.notes.Insert(SimaiDeserializer.noteIndex++, SimaiDeserializer.note);
        SimaiDeserializer.note = new Note();
        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        SlidePath CreateNewSlidePath(bool noIntroAnimation) => new()
        {
            noIntroAnimation = noIntroAnimation,
            vertices         = { noteLocation }
        };
    }

    internal static int ConsumeLocation(ReadOnlySpan<byte> bytes, byte currentByte)
    {
        if (currentByte is >= Constants.ButtonCharStart and <= Constants.ButtonCharEnd
                           or >= Constants.SensorCharStart and <= Constants.SensorCharEndOrEof)
            return ConsumeLocationDirect(bytes, currentByte);

        SimaiDeserializer.ThrowContext<TypeMismatchException>();
        return -1;
    }

    private static int ConsumeLocationDirect(ReadOnlySpan<byte> bytes, byte currentByte)
    {
        if (currentByte < Constants.SensorCharStart)
            return currentByte - Constants.ButtonCharStart;

        var result = ((currentByte - Constants.SensorCharStart) << 4) + 0xa0;

        currentByte = SimaiDeserializer.Pop(bytes);

        if (SimaiDeserializer.isEndOfFile)
            SimaiDeserializer.ThrowContext<ChartFormatException>();

        // Use case: C sensor locations (button is omitted)
        if (currentByte is < Constants.ButtonCharStart or > Constants.ButtonCharEnd)
        {
            SimaiDeserializer.charIndex--;
            return result;
        }

        if (result != 0xC0)
            result += currentByte - Constants.ButtonCharStart;

        return result;
    }

    private static void ConsumeNoteDuration(ReadOnlySpan<byte> bytes)
    {
        var (startLine, startColumn) = SimaiDeserializer.GetCurrentPosition();

        byte currentByte;
        var  startInclusive = SimaiDeserializer.charIndex;
        var  hashIndex      = -1;
        var  colonIndex     = -1;
        var  localTempo     = SimaiDeserializer.tempo;

        do
        {
            currentByte = SimaiDeserializer.Pop(bytes);

            if (SimaiDeserializer.isEndOfFile)
                SimaiDeserializer.ThrowContext<ChartFormatException>();

            switch (currentByte)
            {
                case Constants.HashChar:
                    hashIndex = SimaiDeserializer.charIndex - 1;
                    break;
                case Constants.ColonChar:
                    colonIndex = SimaiDeserializer.charIndex - 1;
                    break;
            }
        } while (currentByte != Constants.DurationCloseChar);

        if (hashIndex == startInclusive)
        {
            if (!bytes[(startInclusive + 1)..(SimaiDeserializer.charIndex - 1)].TryParseFloat(out var result))
                SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

            SimaiDeserializer.note.length = result;
            return;
        }

        if (hashIndex != -1)
        {
            if (!bytes[startInclusive..hashIndex].TryParseFloat(out var tempoResult))
                SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

            localTempo.tempo = tempoResult;
            startInclusive   = hashIndex + 1;
        }

        if (colonIndex == -1)
            SimaiDeserializer.ThrowContext<ChartFormatException>(startLine, startColumn);

        if (!bytes[startInclusive..colonIndex].TryParseFloat(out var nominator))
            SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

        if (!bytes[(colonIndex + 1)..(SimaiDeserializer.charIndex - 1)].TryParseFloat(out var denominator))
            SimaiDeserializer.ThrowContext<TypeMismatchException>(startLine, startColumn);

        SimaiDeserializer.note.length = localTempo.SecondsPerBar / (nominator / 4) * denominator;
    }
}
