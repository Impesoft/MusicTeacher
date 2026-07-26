namespace MusicTeacher.Shared.Practice;

public sealed class TimedWrittenMeasureEvaluator(
    IReadOnlyList<WrittenNoteTarget> targets,
    TimeSpan beatInterval,
    TimeSpan? onsetTolerance = null,
    TimeSpan? durationTolerance = null)
{
    private readonly TimeSpan tolerance = onsetTolerance ?? TimeSpan.FromMilliseconds(240);
    private int? heldMidiNote;

    public int Position { get; private set; }
    public int Mistakes { get; private set; }
    public bool IsComplete => Position >= targets.Count;

    public TimedMeasureStartStep StartNote(int midiNote, TimeSpan elapsed)
    {
        if (IsComplete)
        {
            return new(TimedMeasureStartResult.AlreadyComplete, Position, Mistakes);
        }

        if (heldMidiNote is not null)
        {
            return new(TimedMeasureStartResult.AlreadyHolding, Position, Mistakes);
        }

        if (midiNote != targets[Position].MidiNote)
        {
            Mistakes++;
            return new(TimedMeasureStartResult.IncorrectPitch, Position, Mistakes);
        }

        heldMidiNote = midiNote;
        var expected = beatInterval * (Position + 1);
        var guidance = elapsed < expected - tolerance
            ? TimedMeasureStartResult.Early
            : elapsed > expected + tolerance
                ? TimedMeasureStartResult.Late
                : TimedMeasureStartResult.OnBeat;
        if (guidance is not TimedMeasureStartResult.OnBeat)
        {
            Mistakes++;
        }

        return new(guidance, Position, Mistakes);
    }

    public TimedMeasureEndStep EndNote(int midiNote, TimeSpan heldDuration)
    {
        if (heldMidiNote != midiNote || IsComplete)
        {
            return new(TimedMeasureEndResult.NotHolding, Position, Mistakes, false);
        }

        heldMidiNote = null;
        var duration = HeldDurationEvaluator.Evaluate(
            targets[Position].DurationBeats,
            heldDuration,
            beatInterval,
            durationTolerance);
        var result = duration.Guidance switch
        {
            HeldDurationGuidance.TooShort => TimedMeasureEndResult.TooShort,
            HeldDurationGuidance.TooLong => TimedMeasureEndResult.TooLong,
            _ => TimedMeasureEndResult.OnTarget
        };
        if (!duration.IsSuccessful)
        {
            Mistakes++;
        }

        Position++;
        return new(result, Position, Mistakes, IsComplete);
    }
}

public readonly record struct TimedMeasureStartStep(
    TimedMeasureStartResult Result,
    int Position,
    int Mistakes);

public readonly record struct TimedMeasureEndStep(
    TimedMeasureEndResult Result,
    int Position,
    int Mistakes,
    bool IsComplete);

public enum TimedMeasureStartResult
{
    AlreadyComplete,
    AlreadyHolding,
    IncorrectPitch,
    Early,
    OnBeat,
    Late
}

public enum TimedMeasureEndResult
{
    NotHolding,
    TooShort,
    OnTarget,
    TooLong
}
