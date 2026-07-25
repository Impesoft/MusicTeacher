using MusicTeacher.Shared.MusicTheory;

namespace MusicTeacher.Shared.Practice;

public sealed record WrittenMeasure
{
    public WrittenMeasure(TimeSignature timeSignature, IReadOnlyList<WrittenMeasureNote> notes)
    {
        ArgumentNullException.ThrowIfNull(timeSignature);
        ArgumentNullException.ThrowIfNull(notes);
        if (notes.Count == 0)
        {
            throw new ArgumentException("A measure needs at least one note.", nameof(notes));
        }

        if (timeSignature.BeatUnit != 4 ||
            notes.Sum(note => note.DurationBeats) != timeSignature.BeatsPerMeasure)
        {
            throw new ArgumentException("The written notes must fill the quarter-note measure.", nameof(notes));
        }

        TimeSignature = timeSignature;
        Notes = notes.ToArray();
    }

    public TimeSignature TimeSignature { get; }
    public IReadOnlyList<WrittenMeasureNote> Notes { get; }

    public PitchDurationSequenceEvaluator CreateEvaluator(TimeSpan beatInterval)
        => new(
            Notes.Select(note => new WrittenNoteTarget(note.Pitch.MidiNote, note.DurationBeats)).ToArray(),
            beatInterval);
}

public readonly record struct WrittenMeasureNote(Pitch Pitch, int DurationBeats);
