namespace MusicTeacher.Shared.Practice;

public sealed class PitchDurationSequenceEvaluator(
    IReadOnlyList<WrittenNoteTarget> notes,
    TimeSpan beatInterval)
{
    private readonly IReadOnlyList<WrittenNoteTarget> notes = notes.Count > 0
        ? notes.ToArray()
        : throw new ArgumentException("A written measure needs at least one note.", nameof(notes));
    private int? activeMidiNote;

    public int Position { get; private set; }
    public int Mistakes { get; private set; }
    public bool IsComplete => Position == notes.Count;

    public PitchDurationStartResult StartNote(int midiNote)
    {
        if (IsComplete)
        {
            return PitchDurationStartResult.AlreadyComplete;
        }

        if (activeMidiNote is not null)
        {
            return PitchDurationStartResult.AlreadyHolding;
        }

        if (midiNote != notes[Position].MidiNote)
        {
            Mistakes++;
            return PitchDurationStartResult.IncorrectPitch;
        }

        activeMidiNote = midiNote;
        return PitchDurationStartResult.Started;
    }

    public PitchDurationStep EndNote(int midiNote, TimeSpan heldDuration)
    {
        if (activeMidiNote != midiNote)
        {
            return new(PitchDurationStepResult.NotHolding, Position, Mistakes, null);
        }

        activeMidiNote = null;
        var duration = HeldDurationEvaluator.Evaluate(
            notes[Position].DurationBeats,
            heldDuration,
            beatInterval);
        if (!duration.IsSuccessful)
        {
            Mistakes++;
            return new(
                duration.Guidance == HeldDurationGuidance.TooShort
                    ? PitchDurationStepResult.TooShort
                    : PitchDurationStepResult.TooLong,
                Position,
                Mistakes,
                duration);
        }

        Position++;
        return new(
            IsComplete ? PitchDurationStepResult.Completed : PitchDurationStepResult.Correct,
            Position,
            Mistakes,
            duration);
    }
}

public readonly record struct WrittenNoteTarget(int MidiNote, int DurationBeats);

public readonly record struct PitchDurationStep(
    PitchDurationStepResult Result,
    int Position,
    int Mistakes,
    HeldDurationResult? Duration);

public enum PitchDurationStartResult
{
    Started,
    IncorrectPitch,
    AlreadyHolding,
    AlreadyComplete
}

public enum PitchDurationStepResult
{
    Correct,
    Completed,
    TooShort,
    TooLong,
    NotHolding
}
