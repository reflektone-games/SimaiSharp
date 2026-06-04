using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using SimaiSharp.ErrorHandling;
using SimaiSharp.Structures;
using SimaiSharp.Utilities;

namespace SimaiSharp
{
    internal static class SimaiDeserializer
    {
        private static int currentIndex;

        private static float       currentTime;
        private static TempoChange currentTempo;
        private static NoteFrame   currentNoteFrame = null!;

        private static int currentLine;
        private static int currentColumn;

        private static bool _isEndOfFile;

        public static SimaiChart Deserialize(ReadOnlySpan<byte> bytes)
        {
            currentIndex = 0;

            currentTime      = 0;
            currentTempo     = new TempoChange();
            currentNoteFrame = new NoteFrame();

            currentLine   = 0;
            currentColumn = 0;

            _isEndOfFile = false;

            var chart = new SimaiChart
            {
                noteFrames   = [],
                tempoChanges = []
            };

            while (currentIndex < bytes.Length && !_isEndOfFile)
                ConsumeNext(bytes, ref chart);

            FlushNoteFrame(chart);
            return chart;
        }

        private static void ConsumeNext(ReadOnlySpan<byte> bytes, ref SimaiChart chart)
        {
            var currentByte = MoveNext(bytes);

            switch (currentByte)
            {
                // A single E without any trailing numbers signify EOF
                case Constants.SensorCharEndOrEof
                    when PeekNext(bytes) is < Constants.ButtonCharStart or > Constants.ButtonCharEnd:
                    _isEndOfFile       = true;
                    chart.finishTiming = currentTime;
                    break;
                case Constants.TimeStepChar:
                    chart.finishTiming = Math.Max(currentTime, chart.finishTiming);
                    FlushNoteFrame(chart);
                    currentTime += currentTempo.SecondsPerBeat;
                    break;
                case Constants.SplitFrameChar:
                    chart.finishTiming = Math.Max(currentTime, chart.finishTiming);
                    FlushNoteFrame(chart);
                    break;
                case Constants.TempoBracketOpenChar:
                    ConsumeTempo(bytes);
                    break;
                case Constants.SubdivisionBracketOpenChar:
                    ConsumeSubdivision(bytes);
                    break;
                case Constants.ForceEachChar:
                    currentNoteFrame.isEach = true;
                    break;
                case >= Constants.ButtonCharStart and <= Constants.ButtonCharEnd
                     or >= Constants.SensorCharStart and <= Constants.SensorCharEndOrEof:
                    ConsumeNote(bytes, currentByte, chart, ref currentNoteFrame);
                    break;
                case Constants.SeparatorChar:
                case Constants.NullChar:
                case Constants.SpaceChar:
                    break;
                default:
                    ThrowContext<UndefinedNotationException>();
                    break;
            }
        }

        private static void FlushNoteFrame(SimaiChart chart)
        {
            // Add any pending tempo changes
            if (chart.tempoChanges.Count == 0 || Math.Abs(chart.tempoChanges[^1].time - currentTempo.time) > float.Epsilon)
                chart.tempoChanges.Add(currentTempo);

            if (currentNoteFrame.notes.Count      == 0 &&
                currentNoteFrame.slidePaths.Count == 0)
                return;

            currentNoteFrame.notes.TrimExcess();
            currentNoteFrame.slidePaths.TrimExcess();
            currentNoteFrame.time = currentTime;

            if (currentNoteFrame.notes.Count > 1)
                currentNoteFrame.isEach = true;

            chart.noteFrames.Add(currentNoteFrame);
            currentNoteFrame = new NoteFrame();
        }

        private static void ConsumeNote(ReadOnlySpan<byte> bytes, byte currentByte, SimaiChart chart, ref NoteFrame noteFrame)
        {
            var noteLocation = ConsumeLocationDirect(bytes, currentByte);

            if (_isEndOfFile)
                ThrowContext<ChartFormatException>();

            var noteExists            = true;
            var forceTapStar          = false;
            var noSlideIntroAnimation = false;
            var note = new Note
            {
                location = noteLocation,
                category = noteLocation.ToNoteGroup() == 0 ? NoteCategory.Tap : NoteCategory.Touch
            };

            SlidePath? slidePath = null;

            while (true)
            {
                currentByte = MoveNext(bytes);

                switch (currentByte)
                {
                    #region Decorators

                    case Constants.FireworkChar:
                        note.styles |= NoteStyles.Fireworks;
                        break;
                    case Constants.BreakChar:
                        if (slidePath != null)
                            slidePath.isBreak = true;
                        else
                            note.category = NoteCategory.Break;
                        break;
                    case Constants.ExChar:
                        note.styles |= NoteStyles.Ex;
                        break;
                    case Constants.MineChar:
                        note.styles |= NoteStyles.Mine;
                        break;
                    case Constants.HoldChar:
                        if (note.category != NoteCategory.Break)
                            note.category = NoteCategory.Hold;
                        note.styles |= NoteStyles.Hold;
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
                        note.styles |= NoteStyles.Star;
                        if (PeekNext(bytes) == Constants.ForceStarChar)
                            note.styles |= NoteStyles.Spinning;
                        break;

                    #endregion

                    case Constants.DurationBracketOpenChar:
                        if (slidePath is not null)
                        {
                            ConsumeSlideDuration(bytes, ref slidePath);
                            chart.finishTiming = Math.Max(currentTime + slidePath.duration, chart.finishTiming);
                        }
                        else
                        {
                            ConsumeNoteDuration(bytes, ref note);
                            chart.finishTiming = Math.Max(currentTime + note.length, chart.finishTiming);
                        }

                        break;

                    case Constants.NewSlideChar:
                        if (slidePath != null && slidePath.segments.Count != 0)
                            noteFrame.slidePaths.Add(slidePath);
                        slidePath = CreateNewSlidePath(noSlideIntroAnimation);
                        break;

                    case Constants.StraightLineChar:
                    case Constants.RingRightChar:
                    case Constants.RingLeftChar:
                    case Constants.RingAutoShortChar:
                    case Constants.CurveCwChar:
                    case Constants.CurveCcwChar:
                    case Constants.FoldChar:
                    case Constants.EdgeFoldChar:
                    case Constants.ZigZagSChar:
                    case Constants.ZigZagZChar:
                    case Constants.FanChar:
                        note.styles |=  NoteStyles.Star;
                        slidePath   ??= CreateNewSlidePath(noSlideIntroAnimation);
                        ConsumeSlide(bytes, currentByte, slidePath);
                        break;

                    default:
                        // Resolve all pending data
                        if (slidePath != null && slidePath.segments.Count != 0)
                            noteFrame.slidePaths.Add(slidePath);

                        currentIndex--;
                        goto FINALIZE;
                }
            }

        FINALIZE:
            if (!noteExists)
                return;

            if (forceTapStar)
                note.styles &= ~NoteStyles.Star;

            if ((note.styles & NoteStyles.Hold) != 0 && slidePath is { segments.Count: > 0 } && note.length == 0)
                note.length = slidePath.delay;

            noteFrame.notes.Add(note);
            return;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            SlidePath CreateNewSlidePath(bool noIntroAnimation) => new()
            {
                noIntroAnimation = noIntroAnimation,
                vertices         = { noteLocation }
            };
        }

        private static void ConsumeSlide(ReadOnlySpan<byte> bytes, byte currentByte, SlidePath slidePath)
        {
            var secondByte = MoveNext(bytes);
            var targetLocation =
                ConsumeLocation(
                    bytes, secondByte is Constants.CurveCwChar or Constants.CurveCcwChar ? MoveNext(bytes) : secondByte);

            if (_isEndOfFile)
                ThrowContext<ChartFormatException>();

            var slideType = currentByte switch
            {
                Constants.StraightLineChar => SlideType.StraightLine,
                Constants.RingRightChar => FromRingRight(slidePath.vertices[^1] & 0b111),
                Constants.RingLeftChar => FromRingLeft(slidePath.vertices[^1] & 0b111),
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

            targetLocation = ConsumeLocation(bytes, MoveNext(bytes));

            if (_isEndOfFile)
                ThrowContext<ChartFormatException>();

            slidePath.vertices.Add(targetLocation);
        }

        private static void ConsumeNoteDuration(ReadOnlySpan<byte> bytes, ref Note note)
        {
            var (startLine, startColumn) = GetCurrentPosition();

            byte currentByte;
            var  startInclusive = currentIndex;
            var  hashIndex      = -1;
            var  colonIndex     = -1;
            var  tempo          = currentTempo;

            do
            {
                currentByte = MoveNext(bytes);

                if (_isEndOfFile)
                    ThrowContext<ChartFormatException>();

                switch (currentByte)
                {
                    case Constants.HashChar:
                        hashIndex = currentIndex - 1;
                        break;
                    case Constants.ColonChar:
                        colonIndex = currentIndex - 1;
                        break;
                }
            } while (currentByte != Constants.DurationBracketCloseChar);

            if (hashIndex == startInclusive)
            {
                if (!TryParseFloat(bytes[(startInclusive + 1)..(currentIndex - 1)], out var result))
                    ThrowContext<TypeMismatchException>(startLine, startColumn);

                note.length = result;
                return;
            }

            if (hashIndex != -1)
            {
                if (!TryParseFloat(bytes[startInclusive..hashIndex], out var localTempo))
                    ThrowContext<TypeMismatchException>(startLine, startColumn);

                tempo.tempo    = localTempo;
                startInclusive = hashIndex + 1;
            }

            if (colonIndex == -1)
                ThrowContext<ChartFormatException>(startLine, startColumn);

            if (!TryParseFloat(bytes[startInclusive..colonIndex], out var nominator))
                ThrowContext<TypeMismatchException>(startLine, startColumn);

            if (!TryParseFloat(bytes[(colonIndex + 1)..(currentIndex - 1)], out var denominator))
                ThrowContext<TypeMismatchException>(startLine, startColumn);

            note.length = tempo.SecondsPerBar / (nominator / 4) * denominator;
        }

        /// <summary>
        /// https://w.atwiki.jp/simai/pages/25.html#id_3afb985d
        /// </summary>
        private static void ConsumeSlideDuration(ReadOnlySpan<byte> bytes, ref SlidePath slidePath)
        {
            var (startLine, startColumn) = GetCurrentPosition();

            byte currentByte;
            var  startInclusive = currentIndex;
            var  hashIndex      = 0;
            var  hashCount      = 0;
            var  colonIndex     = -1;
            var  tempo          = currentTempo;

            do
            {
                currentByte = MoveNext(bytes);

                if (_isEndOfFile)
                    ThrowContext<ChartFormatException>(startLine, startColumn);

                switch (currentByte)
                {
                    case Constants.HashChar:
                        if (hashCount == 0)
                            hashIndex = currentIndex - 1;
                        hashCount++;
                        break;
                    case Constants.ColonChar:
                        colonIndex = currentIndex - 1;
                        break;
                }
            } while (currentByte != Constants.DurationBracketCloseChar);

            switch (hashCount)
            {
                // [3##1.5]
                case 2:
                {
                    if (hashIndex == startInclusive)
                        slidePath.delay = tempo.SecondsPerBar;
                    else if (TryParseFloat(bytes[startInclusive..hashIndex], out var delay))
                        slidePath.delay = delay;
                    else
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    if (!TryParseFloat(bytes[(hashIndex + hashCount)..(currentIndex - 1)], out var duration))
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    slidePath.duration += duration;
                    break;
                }
                // [160#2]
                case 1 when colonIndex == -1:
                {
                    if (hashIndex == startInclusive)
                        slidePath.delay = tempo.SecondsPerBar;
                    else if (TryParseFloat(bytes[startInclusive..hashIndex], out var newTempo))
                    {
                        tempo.tempo     = newTempo;
                        slidePath.delay = tempo.SecondsPerBar;
                    }
                    else
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    if (!TryParseFloat(bytes[(hashIndex + 1)..], out var duration))
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    slidePath.duration += duration;
                    break;
                }
                // [160#8:3]
                case 1:
                {
                    if (hashIndex == startInclusive)
                        slidePath.delay = tempo.SecondsPerBar;
                    else if (TryParseFloat(bytes[startInclusive..hashIndex], out var newTempo))
                    {
                        tempo.tempo     = newTempo;
                        slidePath.delay = tempo.SecondsPerBar;
                    }
                    else
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    if (!TryParseFloat(bytes[(hashIndex + hashCount)..colonIndex], out var nominator))
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    if (!TryParseFloat(bytes[(colonIndex + 1)..(currentIndex - 1)], out var denominator))
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    slidePath.duration += tempo.SecondsPerBar / (nominator / 4) * denominator;
                    break;
                }
                // [8:3]
                case 0 when colonIndex != -1:
                {
                    if (!TryParseFloat(bytes[startInclusive..colonIndex], out var nominator))
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    if (!TryParseFloat(bytes[(colonIndex + 1)..(currentIndex - 1)], out var denominator))
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    slidePath.delay    =  tempo.SecondsPerBar;
                    slidePath.duration += tempo.SecondsPerBar / (nominator / 4) * denominator;
                    break;
                }
                default:
                {
                    if (colonIndex == -1)
                        ThrowContext<ChartFormatException>(startLine, startColumn);
                    break;
                }
            }
        }

        private static void ConsumeTempo(ReadOnlySpan<byte> bytes)
        {
            var (startLine, startColumn) = GetCurrentPosition();
            byte currentByte;

            var startInclusive = currentIndex;
            do
            {
                currentByte = MoveNext(bytes);

                if (_isEndOfFile)
                    ThrowContext<ChartFormatException>();
            } while (currentByte != Constants.TempoBracketCloseChar);

            if (!TryParseFloat(bytes[startInclusive..(currentIndex - 1)], out var result))
                ThrowContext<TypeMismatchException>(startLine, startColumn);

            currentTempo.time  = currentTime;
            currentTempo.tempo = result;
        }

        private static void ConsumeSubdivision(ReadOnlySpan<byte> bytes)
        {
            var (startLine, startColumn) = GetCurrentPosition();
            var startInclusive    = currentIndex;
            var explicitTempoMode = false;

            // Consume the first char to see if we need to enable explicitTempoMode
            var currentByte = MoveNext(bytes);

            if (_isEndOfFile)
                ThrowContext<ChartFormatException>();

            if (currentByte == Constants.HashChar)
            {
                explicitTempoMode = true;
                startInclusive++;
            }

            do
            {
                currentByte = MoveNext(bytes);

                if (_isEndOfFile)
                    ThrowContext<ChartFormatException>();
            } while (currentByte != Constants.SubdivisionBracketCloseChar);

            if (!TryParseFloat(bytes[startInclusive..(currentIndex - 1)], out var result))
                ThrowContext<TypeMismatchException>(startLine, startColumn);

            currentTempo.time = currentTime;

            if (explicitTempoMode)
                currentTempo.SetSeconds(result);
            else
                currentTempo.subdivisions = result;
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

        private static int ConsumeLocationDirect(ReadOnlySpan<byte> bytes, byte currentByte)
        {
            if (currentByte < Constants.SensorCharStart)
                return currentByte - Constants.ButtonCharStart;

            var result = ((currentByte - Constants.SensorCharStart) << 4) + 0xa0;

            currentByte = MoveNext(bytes);

            if (_isEndOfFile)
                ThrowContext<ChartFormatException>();

            // Use case: C sensor locations (button is omitted)
            if (currentByte is < Constants.ButtonCharStart or > Constants.ButtonCharEnd)
            {
                currentIndex--;
                return result;
            }

            if (result != 0xC0)
                result += currentByte - Constants.ButtonCharStart;

            return result;
        }

        private static int ConsumeLocation(ReadOnlySpan<byte> bytes, byte currentByte)
        {
            if (currentByte is >= Constants.ButtonCharStart and <= Constants.ButtonCharEnd
                               or >= Constants.SensorCharStart and <= Constants.SensorCharEndOrEof)
                return ConsumeLocationDirect(bytes, currentByte);

            ThrowContext<TypeMismatchException>();
            return -1;
        }

        private static bool TryParseFloat(ReadOnlySpan<byte> utf8Bytes, out float value)
        {
            // The resulting char array will have a maximum length of utf8Bytes.Length,
            // But a mismatch may happen due to code points.
            Span<char>         chars     = stackalloc char[utf8Bytes.Length];
            var                charCount = Encoding.UTF8.GetChars(utf8Bytes, chars);
            ReadOnlySpan<char> charSpan  = chars[..charCount];
            return float.TryParse(charSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static void ThrowContext<T>() where T : SimaiException, new() =>
            throw new T { column = currentColumn, line = currentLine };

        private static void ThrowContext<T>(int line, int column) where T : SimaiException, new() =>
            throw new T { column = column, line = line };

        /// <summary>
        /// Responsible for stripping comments and keeping track of the current line and column
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static byte MoveNext(ReadOnlySpan<byte> bytes)
        {
            byte currentByte;
            var  commentCharCount = 0;
            do
            {
                if (currentIndex >= bytes.Length)
                {
                    _isEndOfFile = true;
                    return 0;
                }

                // Reads the current index, then increment
                currentByte = bytes[currentIndex++];
                currentColumn++;

                switch (currentByte)
                {
                    case Constants.LineFeedChar:
                        currentColumn = 0;
                        currentLine++;
                        commentCharCount = 0;
                        continue;
                    case Constants.SingleLineCommentChar:
                        commentCharCount++;
                        break;
                }
            } while (currentByte is Constants.CarriageReturnChar or Constants.LineFeedChar or Constants.SingleLineCommentChar ||
                     commentCharCount >= 2);

            return currentByte;
        }

        private static byte PeekNext(ReadOnlySpan<byte> bytes)
        {
            if (currentIndex >= bytes.Length)
                return 0;
            return bytes[currentIndex];
        }

        private static (int line, int column) GetCurrentPosition() => (currentLine, currentColumn);
    }
}
