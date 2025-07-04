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
        private static int currentIndex;

        private static float       currentTime;
        private static TempoChange currentTempo;
        private static NoteFrame   currentNoteFrame = null!;

        private static int currentLine   = 1;
        private static int currentColumn = 1;

        private static bool _forceEachApplied;
        private static bool _isEndOfFile;

        public static SimaiChart Deserialize(Span<byte> bytes)
        {
            currentIndex = 0;

            currentTime      = 0;
            currentTempo     = new TempoChange();
            currentNoteFrame = new NoteFrame();

            currentLine   = 1;
            currentColumn = 1;

            _isEndOfFile = false;

            var chart = new SimaiChart
            {
                noteFrames   = new List<NoteFrame>(),
                tempoChanges = new List<TempoChange>()
            };

            while (currentIndex < bytes.Length && !_isEndOfFile)
                ConsumeNext(bytes, ref chart);

            chart.finishTiming = currentTime;
            FlushNoteFrame(chart);

            return chart;
        }

        private static void ConsumeNext(Span<byte> bytes, ref SimaiChart chart)
        {
            var currentByte = MoveNext(bytes);

            switch (currentByte)
            {
                // A single E without any trailing numbers signify EOF
                case SensorCharEndOrEof when PeekNext(bytes) is < ButtonCharStart or > ButtonCharEnd:
                case SplitFrameChar:
                    if (currentByte == SensorCharEndOrEof)
                        _isEndOfFile = true;
                    FlushNoteFrame(chart);
                    break;
                case TempoBracketOpen:
                    ConsumeTempo(bytes);
                    break;
                case SubdivisionBracketOpen:
                    ConsumeSubdivision(bytes);
                    break;
                case ForceEachChar:
                    currentNoteFrame.isEach = true;
                    _forceEachApplied       = true;
                    break;
                case ForceNonEachChar:
                    currentNoteFrame.isEach = false;
                    _forceEachApplied       = true;
                    break;
                case >= ButtonCharStart and <= ButtonCharEnd or >= SensorCharStart and <= SensorCharEndOrEof:
                    ConsumeNote(bytes, currentByte, ref currentNoteFrame);
                    break;
                case SeparatorChar:
                case NullChar:
                case SpaceChar:
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

            if (currentNoteFrame.notes.Count      != 0 ||
                currentNoteFrame.slidePaths.Count != 0)
            {
                currentNoteFrame.notes.TrimExcess();
                currentNoteFrame.slidePaths.TrimExcess();
                currentNoteFrame.time = currentTime;

                if (!_forceEachApplied)
                {
                    if (currentNoteFrame.notes.Count > 1)
                        currentNoteFrame.isEach = true;
                }

                chart.noteFrames.Add(currentNoteFrame);
                currentNoteFrame  = new NoteFrame();
                _forceEachApplied = false;
            }

            currentTime += currentTempo.SecondsPerBeat;
        }

        private static void ConsumeNote(Span<byte> bytes, byte currentByte, ref NoteFrame noteFrame)
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
            };

            SlidePath? slidePath = null;

            while (true)
            {
                currentByte = MoveNext(bytes);

                switch (currentByte)
                {
                    #region Decorators

                    case FireworkChar:
                        note.styles |= NoteStyles.Fireworks;
                        break;
                    case BreakChar:
                        if (slidePath != null)
                            slidePath.isBreak = true;
                        else
                            note.category = NoteCategory.Break;
                        break;
                    case ExChar:
                        note.styles |= NoteStyles.Ex;
                        break;
                    case MineChar:
                        note.styles |= NoteStyles.Mine;
                        break;
                    case HoldChar:
                        if (note.category != NoteCategory.Break)
                            note.category = NoteCategory.Hold;
                        note.styles |= NoteStyles.Hold;
                        break;
                    case TapRemovedSlideChar:
                        noteExists = false;
                        break;
                    case SuddenSlideChar:
                        noteExists            = false;
                        noSlideIntroAnimation = true;

                        if (slidePath != null && slidePath.segments.Count != 0)
                            slidePath.noIntroAnimation = true;
                        break;
                    case ForceNonStarChar:
                        forceTapStar = true;
                        break;
                    case ForceStarChar:
                        note.styles |= NoteStyles.Star;
                        if (PeekNext(bytes) == ForceStarChar)
                            note.styles |= NoteStyles.Spinning;
                        break;

                    #endregion

                    case DurationBracketOpen:
                        if (slidePath is not null)
                            ConsumeSlideDuration(bytes, ref slidePath);
                        else ConsumeNoteDuration(bytes, ref note);
                        break;

                    case NewSlideChar:
                        if (slidePath != null && slidePath.segments.Count != 0)
                            noteFrame.slidePaths.Add(slidePath);
                        slidePath = CreateNewSlidePath(noSlideIntroAnimation);
                        break;

                    case StraightLineChar:
                    case RingRightChar:
                    case RingLeftChar:
                    case RingAutoShortChar:
                    case CurveCwChar:
                    case CurveCcwChar:
                    case FoldChar:
                    case EdgeFoldChar:
                    case ZigZagSChar:
                    case ZigZagZChar:
                    case FanChar:
                        note.styles |= NoteStyles.Star;

                        slidePath ??= CreateNewSlidePath(noSlideIntroAnimation);
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

            noteFrame.notes.Add(note);
            return;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            SlidePath CreateNewSlidePath(bool noIntroAnimation) => new()
            {
                noIntroAnimation = noIntroAnimation,
                vertices         = { noteLocation }
            };
        }

        private static void ConsumeSlide(Span<byte> bytes, byte currentByte, SlidePath slidePath)
        {
            var secondByte = MoveNext(bytes);
            var targetLocation =
                ConsumeLocation(bytes, secondByte is CurveCwChar or CurveCcwChar ? MoveNext(bytes) : secondByte);

            if (_isEndOfFile)
                ThrowContext<ChartFormatException>();

            var slideType = currentByte switch
            {
                StraightLineChar                             => SlideType.StraightLine,
                RingRightChar                                => FromRingRight(slidePath.vertices[^1]),
                RingLeftChar                                 => FromRingLeft(slidePath.vertices[^1]),
                RingAutoShortChar                            => FromRingShortest(slidePath.vertices[^1], targetLocation),
                CurveCwChar when secondByte is CurveCwChar   => SlideType.EdgeCurveCw,
                CurveCwChar                                  => SlideType.CurveCw,
                CurveCcwChar when secondByte is CurveCcwChar => SlideType.EdgeCurveCcw,
                CurveCcwChar                                 => SlideType.CurveCcw,
                FoldChar                                     => SlideType.Fold,
                EdgeFoldChar                                 => SlideType.EdgeFold,
                ZigZagSChar                                  => SlideType.ZigZagS,
                ZigZagZChar                                  => SlideType.ZigZagZ,
                FanChar                                      => SlideType.Fan,
                _                                            => SlideType.StraightLine
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

        private static void ConsumeNoteDuration(Span<byte> bytes, ref Note note)
        {
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
                    case HashChar:
                        hashIndex = currentIndex - 1;
                        break;
                    case ColonChar:
                        colonIndex = currentIndex - 1;
                        break;
                }
            } while (currentByte != DurationBracketClose);

            if (hashIndex == startInclusive)
            {
                if (!TryParseFloat(bytes[(startInclusive + 1)..(currentIndex - 1)], out var result))
                    ThrowContext<TypeMismatchException>();

                note.length = result;
            }
            else if (hashIndex != -1)
            {
                if (!TryParseFloat(bytes[startInclusive..hashIndex], out var localTempo))
                    ThrowContext<TypeMismatchException>();

                tempo.tempo    = localTempo;
                startInclusive = hashIndex + 1;
            }

            if (colonIndex == -1)
                ThrowContext<ChartFormatException>(startInclusive);

            if (!TryParseFloat(bytes[startInclusive..colonIndex], out var nominator))
                ThrowContext<TypeMismatchException>(startInclusive);

            if (!TryParseFloat(bytes[(colonIndex + 1)..(currentIndex - 1)], out var denominator))
                ThrowContext<TypeMismatchException>(colonIndex + 1);

            note.length = tempo.SecondsPerBar / (nominator / 4) * denominator;
        }

        /// <summary>
        /// https://w.atwiki.jp/simai/pages/25.html#id_3afb985d
        /// </summary>
        private static void ConsumeSlideDuration(Span<byte> bytes, ref SlidePath slidePath)
        {
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
                    ThrowContext<ChartFormatException>();

                switch (currentByte)
                {
                    case HashChar:
                        if (hashCount == 0)
                            hashIndex = currentIndex - 1;
                        hashCount++;
                        break;
                    case ColonChar:
                        colonIndex = currentIndex - 1;
                        break;
                }
            } while (currentByte != DurationBracketClose);

            switch (hashCount)
            {
                // [3##1.5]
                case 2:
                {
                    if (!TryParseFloat(bytes[startInclusive..hashIndex], out var delay))
                        ThrowContext<TypeMismatchException>();

                    if (!TryParseFloat(bytes[(hashIndex + hashCount)..(currentIndex - 1)], out var duration))
                        ThrowContext<TypeMismatchException>(startInclusive);

                    slidePath.delay    =  delay;
                    slidePath.duration += duration;
                    break;
                }
                // [160#2]
                case 1 when colonIndex == -1:
                {
                    if (!TryParseFloat(bytes[startInclusive..hashIndex], out var newTempo))
                        ThrowContext<TypeMismatchException>();

                    if (!TryParseFloat(bytes[(hashIndex + 1)..], out var duration))
                        ThrowContext<TypeMismatchException>();

                    tempo.tempo = newTempo;

                    slidePath.delay    =  tempo.SecondsPerBar;
                    slidePath.duration += duration;
                    break;
                }
                // [160#8:3]
                case 1:
                {
                    if (!TryParseFloat(bytes[startInclusive..hashIndex], out var result))
                        ThrowContext<TypeMismatchException>();

                    tempo.tempo = result;

                    if (!TryParseFloat(bytes[(hashIndex + hashCount)..colonIndex], out var nominator))
                        ThrowContext<TypeMismatchException>(startInclusive);

                    if (!TryParseFloat(bytes[(colonIndex + 1)..(currentIndex - 1)], out var denominator))
                        ThrowContext<TypeMismatchException>(colonIndex + 1);

                    slidePath.delay    =  tempo.SecondsPerBar;
                    slidePath.duration += tempo.SecondsPerBar / (nominator / 4) * denominator;
                    break;
                }
                // [8:3]
                case 0 when colonIndex != -1:
                {
                    if (!TryParseFloat(bytes[startInclusive..colonIndex], out var nominator))
                        ThrowContext<TypeMismatchException>(startInclusive);

                    if (!TryParseFloat(bytes[(colonIndex + 1)..(currentIndex - 1)], out var denominator))
                        ThrowContext<TypeMismatchException>(colonIndex + 1);

                    slidePath.delay    =  tempo.SecondsPerBar;
                    slidePath.duration += tempo.SecondsPerBar / (nominator / 4) * denominator;
                    break;
                }
                default:
                {
                    if (colonIndex == -1)
                        ThrowContext<ChartFormatException>(startInclusive);
                    break;
                }
            }
        }

        private static void ConsumeTempo(Span<byte> bytes)
        {
            byte currentByte;
            var  startInclusive = currentIndex;
            do
            {
                currentByte = MoveNext(bytes);

                if (_isEndOfFile)
                    ThrowContext<ChartFormatException>();
            } while (currentByte != TempoBracketClose);

            if (!TryParseFloat(bytes[startInclusive..(currentIndex - 1)], out var result))
                ThrowContext<TypeMismatchException>(startInclusive);

            currentTempo.time  = currentTime;
            currentTempo.tempo = result;
        }

        private static void ConsumeSubdivision(Span<byte> bytes)
        {
            var startInclusive    = currentIndex;
            var explicitTempoMode = false;

            // Consume the first char to see if we need to enable explicitTempoMode
            var currentByte = MoveNext(bytes);

            if (_isEndOfFile)
                ThrowContext<ChartFormatException>();

            if (currentByte == HashChar)
            {
                explicitTempoMode = true;
                startInclusive++;
            }

            do
            {
                currentByte = MoveNext(bytes);

                if (_isEndOfFile)
                    ThrowContext<ChartFormatException>();
            } while (currentByte != SubdivisionBracketClose);

            if (!TryParseFloat(bytes[startInclusive..(currentIndex - 1)], out var result))
                ThrowContext<TypeMismatchException>(startInclusive);

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

        private static int ConsumeLocationDirect(Span<byte> bytes, byte currentByte)
        {
            if (currentByte < SensorCharStart)
                return currentByte - ButtonCharStart;

            var result = ((currentByte - SensorCharStart) << 4) + 0xa0;

            currentByte = MoveNext(bytes);

            if (_isEndOfFile)
                ThrowContext<ChartFormatException>();

            // Use case: C sensor locations (button is omitted)
            if (currentByte is < ButtonCharStart or > ButtonCharEnd)
            {
                currentIndex--;
                return result;
            }

            result += currentByte - ButtonCharStart;
            return result;
        }

        private static int ConsumeLocation(Span<byte> bytes, byte currentByte)
        {
            if (currentByte is >= ButtonCharStart and <= ButtonCharEnd or >= SensorCharStart and <= SensorCharEndOrEof)
                return ConsumeLocationDirect(bytes, currentByte);

            ThrowContext<TypeMismatchException>();
            return -1;
        }

        private static bool TryParseFloat(Span<byte> utf8Bytes, out float value)
        {
            // The resulting char array will have a maximum length of utf8Bytes.Length,
            // But a mismatch may happen due to code points.
            Span<char>         chars     = stackalloc char[utf8Bytes.Length];
            var                charCount = Encoding.UTF8.GetChars(utf8Bytes, chars);
            ReadOnlySpan<char> charSpan  = chars[..charCount];
            return float.TryParse(charSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static void ThrowContext<T>(int line = -1, int column = -1) where T : SimaiException, new()
        {
            if (line   == -1) line   = currentLine;
            if (column == -1) column = currentColumn;
            throw new T { column = column, line = line };
        }

        /// <summary>
        /// Responsible for stripping comments and keeping track of the current line and column
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static byte MoveNext(Span<byte> bytes)
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

                if (currentByte == '\n')
                {
                    currentColumn = 1;
                    currentLine++;
                    commentCharCount = 0;
                    continue;
                }

                if (currentByte == SingleLineCommentChar)
                    commentCharCount++;
            } while (currentByte is CarriageReturnChar or LineFeedChar or SingleLineCommentChar ||
                     commentCharCount >= 2);

            return currentByte;
        }

        private static byte PeekNext(Span<byte> bytes)
        {
            if (currentIndex >= bytes.Length)
                return 0;
            return bytes[currentIndex];
        }

        #region Constants

        private const byte SpaceChar          = (byte)' ';
        private const byte NullChar           = (byte)'\0';
        private const byte CarriageReturnChar = (byte)'\r';
        private const byte LineFeedChar       = (byte)'\n';

        private const byte TempoBracketOpen  = (byte)'(';
        private const byte TempoBracketClose = (byte)')';

        private const byte SubdivisionBracketOpen  = (byte)'{';
        private const byte SubdivisionBracketClose = (byte)'}';

        private const byte DurationBracketOpen  = (byte)'[';
        private const byte DurationBracketClose = (byte)']';

        private const byte HashChar              = (byte)'#';
        private const byte ColonChar             = (byte)':';
        private const byte SingleLineCommentChar = (byte)'|';

        private const byte SplitFrameChar = (byte)',';
        private const byte SeparatorChar  = (byte)'/';

        private const byte ForceEachChar    = (byte)'0';
        private const byte ForceNonEachChar = (byte)'`';

        private const byte ButtonCharStart = (byte)'1';
        private const byte ButtonCharEnd   = (byte)'8';

        private const byte SensorCharStart    = (byte)'A';
        private const byte SensorCharEndOrEof = (byte)'E';

        private const byte FireworkChar        = (byte)'f';
        private const byte BreakChar           = (byte)'b';
        private const byte ExChar              = (byte)'x';
        private const byte MineChar            = (byte)'m';
        private const byte HoldChar            = (byte)'h';
        private const byte TapRemovedSlideChar = (byte)'?';
        private const byte SuddenSlideChar     = (byte)'!';

        private const byte ForceStarChar    = (byte)'$';
        private const byte ForceNonStarChar = (byte)'@';

        private const byte NewSlideChar = (byte)'*';

        private const byte StraightLineChar = (byte)'-';

        private const byte RingRightChar     = (byte)'>';
        private const byte RingLeftChar      = (byte)'<';
        private const byte RingAutoShortChar = (byte)'^';

        private const byte CurveCwChar  = (byte)'q';
        private const byte CurveCcwChar = (byte)'p';

        private const byte FoldChar     = (byte)'v';
        private const byte EdgeFoldChar = (byte)'V';

        private const byte ZigZagSChar = (byte)'s';
        private const byte ZigZagZChar = (byte)'z';

        private const byte FanChar = (byte)'w';

        #endregion
    }
}
