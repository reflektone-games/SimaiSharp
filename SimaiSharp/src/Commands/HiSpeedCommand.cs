using System;
using System.Globalization;
using System.Text;
using SimaiSharp.Structures;

namespace SimaiSharp.Commands
{
    public sealed class HiSpeedCommand : Command
    {
        public override void Deserialize(ReadOnlySpan<byte> chartSpan)
        {
            Span<char> chars     = stackalloc char[chartSpan.Length];
            var        charCount = Encoding.UTF8.GetChars(chartSpan, chars);
            var        charSpan  = chars[..charCount];

            var value = double.Parse(charSpan, NumberStyles.Float, CultureInfo.InvariantCulture);

            var existingNoteGroupIndex =
                SimaiDeserializer.chart.noteGroups.FindIndex(n => n.scaleChanges.Count == 0 &&
                                                                  n.paceChanges.Count  == 1 &&
                                                                  n.paceChanges[0].value
                                                                   .Equals(value) &&
                                                                  n.paceChanges[0].time == 0);

            var noteGroup = SimaiDeserializer.NextNoteGroup(existingNoteGroupIndex);

            if (existingNoteGroupIndex != -1)
                return;

            noteGroup.paceChanges[0] = new State
            {
                time  = 0,
                value = value
            };
        }
    }
}
