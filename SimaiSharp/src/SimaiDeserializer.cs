using System;
using System.Collections.Generic;
using SimaiSharp.ErrorHandling;
using SimaiSharp.Processors;
using SimaiSharp.Structures;
using SimaiSharp.Utilities;

namespace SimaiSharp
{
    internal static class SimaiDeserializer
    {
        public static readonly Dictionary<int, int>    NoteGroupAliases = new();
        public static readonly Dictionary<int, double> JumpMarkers      = new();

        internal static int  charIndex;
        internal static int  noteIndex;
        internal static int  slideIndex;
        internal static bool isEndOfFile;
        internal static Note firstNoteOfFrame = null!;

        internal static double      time;
        internal static SimaiChart  chart = null!;
        internal static TempoChange tempo;
        internal static Note        note      = null!;
        internal static NoteGroup   noteGroup = null!;
        internal static bool        noteGroupAddedToChart;

        private static int currentLine;
        private static int currentColumn;

        private static ChartHasher _hasher = new();

        public static SimaiChart Deserialize(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0)
                return new SimaiChart();

            NoteGroupAliases.Clear();
            JumpMarkers.Clear();
            charIndex  = 0;
            noteIndex  = 0;
            slideIndex = 0;

            time                  = 0;
            tempo                 = new TempoChange();
            noteGroup             = new NoteGroup();
            chart                 = new SimaiChart();
            note                  = new Note();
            noteGroupAddedToChart = false;

            currentLine   = 0;
            currentColumn = 0;

            isEndOfFile = false;
            _hasher.Clear();

            while (charIndex < bytes.Length && !isEndOfFile)
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
                    isEndOfFile = true;
                    return;
                case Constants.TimeStepChar:
                    SetTime(time + tempo.SecondsPerBeat);
                    break;
                case Constants.SplitFrameChar:
                    firstNoteOfFrame.styles |= NoteStyles.ForceSingleFrame;
                    firstNoteOfFrame.styles &= ~NoteStyles.ForceEachFrame;
                    break;
                case Constants.TempoBracketOpenChar:
                    TempoProcessor.Deserialize(bytes);
                    break;
                case Constants.SubdivisionOpenChar:
                    TempoProcessor.ConsumeSubdivision(bytes);
                    break;
                case Constants.RingLeftOrCommandOpenChar:
                    CommandProcessor.Deserialize(bytes);
                    break;
                case Constants.ForceEachChar:
                    firstNoteOfFrame.styles |= NoteStyles.ForceEachFrame;
                    break;
                case >= Constants.ButtonCharStart and <= Constants.ButtonCharEnd
                     or >= Constants.SensorCharStart and <= Constants.SensorCharEndOrEof:
                    NoteProcessor.Deserialize(bytes, currentByte);
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

        internal static void ThrowContext<T>() where T : SimaiException, new() =>
            throw new T { column = currentColumn, line = currentLine };

        internal static void ThrowContext<T>(int line, int column) where T : SimaiException, new() =>
            throw new T { column = column, line = line };

        /// <summary>
        /// Responsible for stripping comments and keeping track of the current line and column
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        internal static byte Pop(ReadOnlySpan<byte> bytes)
        {
            byte currentByte;
            var  commentCharCount = 0;
            do
            {
                if (charIndex >= bytes.Length)
                {
                    isEndOfFile = true;
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

        internal static byte Peek(ReadOnlySpan<byte> bytes)
        {
            if (charIndex >= bytes.Length)
                return 0;
            return bytes[charIndex];
        }

        internal static (int line, int column) GetCurrentPosition() => (currentLine, currentColumn);

        // TODO)) Improve performance by seeding initial search index
        public static void SetTime(double value)
        {
            noteIndex        = FindInsertIndexForTime(noteGroup.notes, value);
            firstNoteOfFrame = note = new Note();
            for (var i = noteIndex - 1; i >= 0; i--)
            {
                var seekNote = noteGroup.notes[i];
                if (seekNote.time < value)
                    break;
                firstNoteOfFrame = seekNote;
            }

            slideIndex = FindInsertIndexForTime(noteGroup.slidePaths, value);
            time       = value;
        }

        private static int FindInsertIndexForTime<T>(IReadOnlyList<T> timedList, double value) where T : ITimed
        {
            if (timedList.Count == 0 || timedList[0].GetTime() > value)
                return 0;

            if (timedList[^1].GetTime() < value)
                return timedList.Count;

            var low  = 0;
            var high = timedList.Count - 1;
            while (low != high)
            {
                var mid = (low + high) / 2;
                if (timedList[mid].GetTime() <= value)
                    low = mid + 1;
                else
                    high = mid;
            }

            return low;
        }
    }
}
