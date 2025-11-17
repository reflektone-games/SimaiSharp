using System.IO;
using SimaiSharp.Structures;

namespace SimaiSharp;

internal static class SimaiSerializer
{
    public static void Serialize(SimaiChart chart, StreamWriter writer)
    {
        var currentTime    = 0f;
        var noteFrameIndex = 0;

        var tempoIndex   = 0;
        var currentTempo = chart.tempoChanges[0];
        var nextTempo    = chart.tempoChanges.Count == 1 ? TempoChange.invalid : chart.tempoChanges[1];

        while (currentTime <= chart.finishTiming)
        {
            if (noteFrameIndex >= chart.noteFrames.Count)
                goto STEP;

            var noteFrame = chart.noteFrames[noteFrameIndex];
            WriteNoteFrame(writer, noteFrame);
            noteFrameIndex++;

        STEP:
            if (nextTempo.IsValid && currentTime >= nextTempo.time)
            {
                tempoIndex++;
                currentTempo = nextTempo;
                nextTempo    = chart.tempoChanges.Count == tempoIndex ? TempoChange.invalid : chart.tempoChanges[tempoIndex];
            }

            currentTime += currentTempo.SecondsPerBeat;
            writer.Write((char)Constants.TimeStepChar);
        }

        writer.Flush();
    }

    private static void WriteNoteFrame(StreamWriter writer, NoteFrame noteFrame)
    {
        foreach (var note in noteFrame.notes)
        {
            writer.Write(note.location + 1);
        }
    }
}
