namespace MusicTeacher.Shared.Practice;

public sealed class RhythmPatternEvaluator(
    IReadOnlyList<int> soundedBeats,
    TimeSpan beatInterval,
    TimeSpan? tolerance = null)
{
    private readonly IReadOnlyList<TimeSpan> expectedOffsets = soundedBeats
        .Select(beat => beatInterval * beat)
        .ToArray();
    private readonly TimeSpan acceptedTolerance = tolerance ?? TimeSpan.FromMilliseconds(190);
    private int position;
    private int mistakes;

    public int Position => position;
    public int ExpectedTapCount => expectedOffsets.Count;
    public int Mistakes => mistakes;

    public RhythmTapStep SubmitTap(TimeSpan elapsed)
    {
        if (position >= expectedOffsets.Count)
        {
            throw new InvalidOperationException("The rhythm pattern is already complete.");
        }

        var difference = elapsed - expectedOffsets[position];
        var guidance = difference < -acceptedTolerance
            ? RhythmTapGuidance.Early
            : difference > acceptedTolerance
                ? RhythmTapGuidance.Late
                : RhythmTapGuidance.OnBeat;

        if (guidance != RhythmTapGuidance.OnBeat)
        {
            mistakes++;
        }

        position++;
        return new RhythmTapStep(
            position,
            expectedOffsets.Count,
            guidance,
            position == expectedOffsets.Count,
            position == expectedOffsets.Count && mistakes == 0);
    }
}

public sealed record RhythmTapStep(
    int Position,
    int ExpectedTapCount,
    RhythmTapGuidance Guidance,
    bool IsComplete,
    bool IsSuccessful);

public enum RhythmTapGuidance
{
    Early,
    OnBeat,
    Late
}
