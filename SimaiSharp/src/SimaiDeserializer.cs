using System;
using System.Collections.Generic;
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
        public static readonly Dictionary<int, int>    NoteGroupAliases = new();
        public static readonly Dictionary<int, double> JumpMarkers      = new();

        private static int  charIndex;
        private static int  noteIndex;
        private static Note firstNoteOfFrame = null!;

        internal static double      time;
        internal static SimaiChart  chart = null!;
        internal static TempoChange tempo;
        internal static Note        note      = null!;
        internal static NoteGroup   noteGroup = null!;
        internal static bool        noteGroupAddedToChart;

        private static int currentLine;
        private static int currentColumn;

        private static bool        _isEndOfFile;
        private static ChartHasher _hasher = new();

        public static SimaiChart Deserialize(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0)
                return new SimaiChart();

            NoteGroupAliases.Clear();
            JumpMarkers.Clear();
            charIndex = 0;
            noteIndex = 0;

            time                  = 0;
            tempo                 = new TempoChange();
            noteGroup             = new NoteGroup();
            chart                 = new SimaiChart();
            note                  = new Note();
            noteGroupAddedToChart = false;

            currentLine   = 0;
            currentColumn = 0;

            _isEndOfFile = false;
            _hasher.Clear();

            while (charIndex < bytes.Length && !_isEndOfFile)
                Consume(bytes);

            NextNoteGroup(0);
            chart.finishTiming = time;
            chart.hash         = _hasher.GetHash();
            foreach (var group in chart.noteGroups)
            {
                group.notes.Sort((a,        b) => a.time.CompareTo(b.time));
                group.slidePaths.Sort((a,   b) => a.time.CompareTo(b.time));
                group.paceChanges.Sort((a,  b) => a.time.CompareTo(b.time));
                group.scaleChanges.Sort((a, b) => a.time.CompareTo(b.time));
            }

            return chart;
        }

        private static void Consume(ReadOnlySpan<byte> bytes)
        {
            var currentByte = Pop(bytes);

            switch (currentByte)
            {
                // A single E without any trailing numbers signify EOF
                case Constants.SensorCharEndOrEof when Peek(bytes) is < Constants.ButtonCharStart or > Constants.ButtonCharEnd:
                    _isEndOfFile = true;
                    return;
                case Constants.TimeStepChar:
                    firstNoteOfFrame =  note;
                    note.styles      =  NoteStyles.None;
                    time             += tempo.SecondsPerBeat;
                    break;
                case Constants.SplitFrameChar:
                    firstNoteOfFrame.styles |= NoteStyles.ForceSingleFrame;
                    firstNoteOfFrame.styles &= ~NoteStyles.ForceEachFrame;
                    break;
                case Constants.TempoBracketOpenChar:
                    ConsumeTempo(bytes);
                    break;
                case Constants.SubdivisionOpenChar:
                    ConsumeSubdivision(bytes);
                    break;
                case Constants.RingLeftOrCommandOpenChar:
                    ConsumeCommand(bytes);
                    break;
                case Constants.ForceEachChar:
                    firstNoteOfFrame.styles |= NoteStyles.ForceEachFrame;
                    break;
                case >= Constants.ButtonCharStart and <= Constants.ButtonCharEnd
                     or >= Constants.SensorCharStart and <= Constants.SensorCharEndOrEof:
                    ConsumeNote(bytes, currentByte);
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


        internal static NoteGroup NextNoteGroup(int setIndex = -1)
        {
            noteGroup.notes.TrimExcess();
            noteGroup.slidePaths.TrimExcess();
            noteGroup.paceChanges.TrimExcess();
            noteGroup.scaleChanges.TrimExcess();

            if (!noteGroupAddedToChart && !noteGroup.IsEmpty)
                chart.noteGroups.Add(noteGroup);

            return setIndex == -1
                ? noteGroup = new NoteGroup
                {
                    paceChanges  = [new State { time = 0, value = 1 }],
                    scaleChanges = [new State { time = 0, value = 1 }]
                }
                : noteGroup = chart.noteGroups[setIndex];
        }

        private static void ConsumeNote(ReadOnlySpan<byte> bytes, byte currentByte)
        {
            var noteLocation = ConsumeLocationDirect(bytes, currentByte);

            if (_isEndOfFile)
                ThrowContext<ChartFormatException>();

            var noteExists            = true;
            var forceTapStar          = false;
            var noSlideIntroAnimation = false;

            note.location = noteLocation;
            note.category = noteLocation.ToNoteGroup() == 0 ? NoteCategory.Tap : NoteCategory.Touch;

            SlidePath? slidePath = null;

            while (true)
            {
                currentByte = Pop(bytes);

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
                        if (slidePath != null)
                            slidePath.isMine = true;
                        else
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
                        if (Peek(bytes) == Constants.ForceStarChar)
                            note.styles |= NoteStyles.Spinning;
                        break;

                    #endregion

                    case Constants.DurationOpenChar:
                        if (slidePath is not null)
                        {
                            ConsumeSlideDuration(bytes, ref slidePath);
                            chart.finishTiming = Math.Max(time + slidePath.duration, chart.finishTiming);
                        }
                        else
                        {
                            ConsumeNoteDuration(bytes);
                            chart.finishTiming = Math.Max(time + note.length, chart.finishTiming);
                        }

                        break;

                    case Constants.NewSlideOrCommandArgumentChar:
                        if (slidePath != null && slidePath.segments.Count != 0)
                            noteGroup.slidePaths.Add(slidePath);
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
                        note.styles |=  NoteStyles.Star;
                        slidePath   ??= CreateNewSlidePath(noSlideIntroAnimation);
                        ConsumeSlide(bytes, currentByte, slidePath);
                        break;

                    default:
                        // Resolve all pending data
                        if (slidePath != null && slidePath.segments.Count != 0)
                            noteGroup.slidePaths.Add(slidePath);

                        charIndex--;
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

            note.time = time;
            noteGroup.notes.Insert(noteIndex, note);
            noteIndex++;
            note = new Note();
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
            var secondByte = Pop(bytes);
            var targetLocation =
                ConsumeLocation(
                    bytes, secondByte is Constants.CurveCwChar or Constants.CurveCcwChar ? Pop(bytes) : secondByte);

            if (_isEndOfFile)
                ThrowContext<ChartFormatException>();

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

            targetLocation = ConsumeLocation(bytes, Pop(bytes));

            if (_isEndOfFile)
                ThrowContext<ChartFormatException>();

            slidePath.vertices.Add(targetLocation);
        }

        private static void ConsumeNoteDuration(ReadOnlySpan<byte> bytes)
        {
            var (startLine, startColumn) = GetCurrentPosition();

            byte currentByte;
            var  startInclusive = charIndex;
            var  hashIndex      = -1;
            var  colonIndex     = -1;
            var  localTempo     = tempo;

            do
            {
                currentByte = Pop(bytes);

                if (_isEndOfFile)
                    ThrowContext<ChartFormatException>();

                switch (currentByte)
                {
                    case Constants.HashChar:
                        hashIndex = charIndex - 1;
                        break;
                    case Constants.ColonChar:
                        colonIndex = charIndex - 1;
                        break;
                }
            } while (currentByte != Constants.DurationCloseChar);

            if (hashIndex == startInclusive)
            {
                if (!TryParseFloat(bytes[(startInclusive + 1)..(charIndex - 1)], out var result))
                    ThrowContext<TypeMismatchException>(startLine, startColumn);

                note.length = result;
                return;
            }

            if (hashIndex != -1)
            {
                if (!TryParseFloat(bytes[startInclusive..hashIndex], out var tempoResult))
                    ThrowContext<TypeMismatchException>(startLine, startColumn);

                localTempo.tempo = tempoResult;
                startInclusive   = hashIndex + 1;
            }

            if (colonIndex == -1)
                ThrowContext<ChartFormatException>(startLine, startColumn);

            if (!TryParseFloat(bytes[startInclusive..colonIndex], out var nominator))
                ThrowContext<TypeMismatchException>(startLine, startColumn);

            if (!TryParseFloat(bytes[(colonIndex + 1)..(charIndex - 1)], out var denominator))
                ThrowContext<TypeMismatchException>(startLine, startColumn);

            note.length = localTempo.SecondsPerBar / (nominator / 4) * denominator;
        }

        /// <summary>
        /// https://w.atwiki.jp/simai/pages/25.html#id_3afb985d
        /// </summary>
        private static void ConsumeSlideDuration(ReadOnlySpan<byte> bytes, ref SlidePath slidePath)
        {
            var (startLine, startColumn) = GetCurrentPosition();

            byte currentByte;
            var  startInclusive = charIndex;
            var  hashIndex      = 0;
            var  hashCount      = 0;
            var  colonIndex     = -1;
            var  localTempo     = tempo;

            do
            {
                currentByte = Pop(bytes);

                if (_isEndOfFile)
                    ThrowContext<ChartFormatException>(startLine, startColumn);

                switch (currentByte)
                {
                    case Constants.HashChar:
                        if (hashCount == 0)
                            hashIndex = charIndex - 1;
                        hashCount++;
                        break;
                    case Constants.ColonChar:
                        colonIndex = charIndex - 1;
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
                    else if (TryParseFloat(bytes[startInclusive..hashIndex], out var delay))
                        slidePath.delay = delay;
                    else
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    if (!TryParseFloat(bytes[(hashIndex + hashCount)..(charIndex - 1)], out var duration))
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    slidePath.duration += duration;
                    break;
                }
                // [160#2]
                case 1 when colonIndex == -1:
                {
                    if (hashIndex == startInclusive)
                        slidePath.delay = localTempo.SecondsPerBar;
                    else if (TryParseFloat(bytes[startInclusive..hashIndex], out var newTempo))
                    {
                        localTempo.tempo = newTempo;
                        slidePath.delay  = localTempo.SecondsPerBar;
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
                        slidePath.delay = localTempo.SecondsPerBar;
                    else if (TryParseFloat(bytes[startInclusive..hashIndex], out var newTempo))
                    {
                        localTempo.tempo = newTempo;
                        slidePath.delay  = localTempo.SecondsPerBar;
                    }
                    else
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    if (!TryParseFloat(bytes[(hashIndex + hashCount)..colonIndex], out var nominator))
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    if (!TryParseFloat(bytes[(colonIndex + 1)..(charIndex - 1)], out var denominator))
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    slidePath.duration += localTempo.SecondsPerBar / (nominator / 4) * denominator;
                    break;
                }
                // [8:3]
                case 0 when colonIndex != -1:
                {
                    if (!TryParseFloat(bytes[startInclusive..colonIndex], out var nominator))
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    if (!TryParseFloat(bytes[(colonIndex + 1)..(charIndex - 1)], out var denominator))
                        ThrowContext<TypeMismatchException>(startLine, startColumn);

                    slidePath.delay    =  localTempo.SecondsPerBar;
                    slidePath.duration += localTempo.SecondsPerBar / (nominator / 4) * denominator;
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

            var startInclusive = charIndex;
            do
            {
                currentByte = Pop(bytes);

                if (_isEndOfFile)
                    ThrowContext<ChartFormatException>();
            } while (currentByte != Constants.TempoBracketCloseChar);

            if (!TryParseFloat(bytes[startInclusive..(charIndex - 1)], out var result))
                ThrowContext<TypeMismatchException>(startLine, startColumn);

            tempo.time  = time;
            tempo.tempo = result;
        }

        private static void ConsumeSubdivision(ReadOnlySpan<byte> bytes)
        {
            var (startLine, startColumn) = GetCurrentPosition();
            var startInclusive    = charIndex;
            var explicitTempoMode = false;

            // Consume the first char to see if we need to enable explicitTempoMode
            var currentByte = Pop(bytes);

            if (_isEndOfFile)
                ThrowContext<ChartFormatException>();

            if (currentByte == Constants.HashChar)
            {
                explicitTempoMode = true;
                startInclusive++;
            }

            do
            {
                currentByte = Pop(bytes);

                if (_isEndOfFile)
                    ThrowContext<ChartFormatException>();
            } while (currentByte != Constants.SubdivisionCloseChar);

            if (!TryParseFloat(bytes[startInclusive..(charIndex - 1)], out var result))
                ThrowContext<TypeMismatchException>(startLine, startColumn);

            tempo.time = time;

            if (explicitTempoMode)
                tempo.SetSeconds(result);
            else
                tempo.subdivisions = result;
        }

        private static void ConsumeCommand(ReadOnlySpan<byte> bytes)
        {
            var (startLine, startColumn) = GetCurrentPosition();
            byte currentByte;

            var startInclusive = charIndex;
            var separatorIndex = -1;
            do
            {
                currentByte = Pop(bytes);

                if (currentByte is Constants.NewSlideOrCommandArgumentChar or Constants.HashChar)
                    separatorIndex = charIndex - 1;

                if (_isEndOfFile)
                    ThrowContext<ChartFormatException>(startLine, startColumn);
            } while (currentByte != Constants.RingRightOrCommandCloseChar);

            if (separatorIndex == -1)
                ThrowContext<ChartFormatException>(startLine, startColumn);

            if (!SimaiConvert.TryGetCommand(bytes[startInclusive..separatorIndex], out var command))
                ThrowContext<ChartFormatException>(startLine, startColumn);

            try
            {
                command.Deserialize(bytes[(separatorIndex + 1)..(charIndex - 1)]);
            }
            catch (Exception)
            {
                ThrowContext<TypeMismatchException>(startLine, startColumn);
            }
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

            currentByte = Pop(bytes);

            if (_isEndOfFile)
                ThrowContext<ChartFormatException>();

            // Use case: C sensor locations (button is omitted)
            if (currentByte is < Constants.ButtonCharStart or > Constants.ButtonCharEnd)
            {
                charIndex--;
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
        private static byte Pop(ReadOnlySpan<byte> bytes)
        {
            byte currentByte;
            var  commentCharCount = 0;
            do
            {
                if (charIndex >= bytes.Length)
                {
                    _isEndOfFile = true;
                    return 0;
                }

                // Reads the current index, then increment
                currentByte = bytes[charIndex++];
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
            } while (currentByte is Constants.CarriageReturnChar
                                    or Constants.LineFeedChar
                                    or Constants.SpaceChar
                                    or Constants.SingleLineCommentChar ||
                     commentCharCount >= 2);

            _hasher.Append(currentByte);
            return currentByte;
        }

        private static byte Peek(ReadOnlySpan<byte> bytes)
        {
            if (charIndex >= bytes.Length)
                return 0;
            return bytes[charIndex];
        }

        private static (int line, int column) GetCurrentPosition() => (currentLine, currentColumn);

        public static void SetTime(double value)
        {
            noteIndex        = FindInsertIndexForTime(value);
            firstNoteOfFrame = note = new Note();
            for (var i = noteIndex - 1; i >= 0; i--)
            {
                var seekNote = noteGroup.notes[i];
                if (seekNote.time < value)
                    break;
                firstNoteOfFrame = seekNote;
            }
        }

        private static int FindInsertIndexForTime(double value)
        {
            if (noteGroup.notes[0].time > value)
                return 0;

            if (noteGroup.notes[^1].time < value)
                return noteGroup.notes.Count;

            var low  = 0;
            var high = noteGroup.notes.Count - 1;
            while (low != high)
            {
                var mid = (low + high) / 2;
                if (noteGroup.notes[mid].time <= value)
                    low = mid + 1;
                else
                    high = mid;
            }

            return low;
        }
    }
}
