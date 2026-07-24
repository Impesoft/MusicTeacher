namespace MusicTeacher.Shared.Practice;

/// <summary>
/// Evaluates an ordered melody by pitch only. Release 1 deliberately has no timing,
/// duration, velocity, or note-off inputs.
/// </summary>
public sealed class PitchSequenceEvaluator
{
    private readonly IReadOnlyList<int> expectedMidiNotes;

    public PitchSequenceEvaluator(IReadOnlyList<int> expectedMidiNotes)
    {
        ArgumentNullException.ThrowIfNull(expectedMidiNotes);
        if (expectedMidiNotes.Count == 0)
        {
            throw new ArgumentException("A pitch sequence needs at least one note.", nameof(expectedMidiNotes));
        }

        if (expectedMidiNotes.Any(note => note is < 0 or > 127))
        {
            throw new ArgumentOutOfRangeException(nameof(expectedMidiNotes), "MIDI notes must be between 0 and 127.");
        }

        this.expectedMidiNotes = expectedMidiNotes.ToArray();
    }

    public IReadOnlyList<int> ExpectedMidiNotes => expectedMidiNotes;
    public int Position { get; private set; }
    public int Mistakes { get; private set; }
    public bool IsComplete => Position == expectedMidiNotes.Count;

    public PitchSequenceStep SubmitNote(int midiNote)
    {
        if (midiNote is < 0 or > 127)
        {
            throw new ArgumentOutOfRangeException(nameof(midiNote), "A MIDI note must be between 0 and 127.");
        }

        if (IsComplete)
        {
            return new PitchSequenceStep(PitchSequenceStepResult.AlreadyComplete, Position, Mistakes);
        }

        if (midiNote != expectedMidiNotes[Position])
        {
            Mistakes++;
            return new PitchSequenceStep(PitchSequenceStepResult.Incorrect, Position, Mistakes);
        }

        Position++;
        return new PitchSequenceStep(
            IsComplete ? PitchSequenceStepResult.Completed : PitchSequenceStepResult.Correct,
            Position,
            Mistakes);
    }

    public void Reset()
    {
        Position = 0;
        Mistakes = 0;
    }
}

public readonly record struct PitchSequenceStep(
    PitchSequenceStepResult Result,
    int Position,
    int Mistakes);

public enum PitchSequenceStepResult
{
    Correct,
    Incorrect,
    Completed,
    AlreadyComplete
}
