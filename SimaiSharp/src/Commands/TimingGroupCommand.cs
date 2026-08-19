using System;
using System.Globalization;
using System.Text;
using SimaiSharp.Structures;

namespace SimaiSharp.Commands
{
    public sealed class TimingGroupCommand : Command
    {
        public override void Deserialize(ReadOnlySpan<byte> chartSpan)
        {
            Span<char> chars     = stackalloc char[chartSpan.Length];
            var        charCount = Encoding.UTF8.GetChars(chartSpan, chars);
            var        charSpan  = chars[..charCount];

            var value = double.Parse(charSpan, NumberStyles.Float, CultureInfo.InvariantCulture);

            var existingNoteGroupIndex = SimaiDeserializer.chart.noteGroups.FindIndex(n => n.speedMultiplierChanges.Count == 0 &&
                n.speedVariationChanges.Count   == 1 &&
                n.speedVariationChanges[0].speed
                 .Equals(value) &&
                n.speedVariationChanges[0].time == 0);

            var noteGroup = SimaiDeserializer.NextNoteGroup(existingNoteGroupIndex);

            if (existingNoteGroupIndex == -1)
                noteGroup.speedVariationChanges.Add(new SpeedChange
                {
                    time  = 0,
                    speed = value
                });
        }
    }
}
