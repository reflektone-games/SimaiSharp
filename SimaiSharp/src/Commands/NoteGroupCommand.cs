using System;
using SimaiSharp.Utilities;

namespace SimaiSharp.Commands
{
    public sealed class NoteGroupCommand : Command
    {
        public override void Deserialize(ReadOnlySpan<byte> chartSpan)
        {
            var key = Hashing.ComputeHash(chartSpan);

            if (SimaiDeserializer.NoteGroupAliases.TryGetValue(key, out var groupIndex))
                SimaiDeserializer.NextNoteGroup(groupIndex);
            else
            {
                SimaiDeserializer.NoteGroupAliases[key] = SimaiDeserializer.chart.noteGroups.Count;
                SimaiDeserializer.NextNoteGroup();
            }
        }
    }
}
