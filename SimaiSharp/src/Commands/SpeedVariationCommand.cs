using System;
using System.Globalization;
using System.Text;
using SimaiSharp.Structures;

namespace SimaiSharp.Commands
{
    public sealed class SpeedVariationCommand : Command
    {
        public override void Deserialize(ReadOnlySpan<byte> chartSpan)
        {
            Span<char> chars     = stackalloc char[chartSpan.Length];
            var        charCount = Encoding.UTF8.GetChars(chartSpan, chars);
            var        charSpan  = chars[..charCount];

            var value = double.Parse(charSpan, NumberStyles.Float, CultureInfo.InvariantCulture);

            SimaiDeserializer.noteGroup.speedVariationChanges.Add(new SpeedChange
            {
                time  = SimaiDeserializer.time,
                speed = value
            });
        }
    }
}
