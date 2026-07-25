namespace MusicTeacher.Shared.Practice;

public sealed class SteadyBeatEvaluator(
    TimeSpan targetInterval,
    int requiredTaps = 8,
    TimeSpan? guidanceTolerance = null)
{
    private readonly List<TimeSpan> taps = [];
    private readonly TimeSpan tolerance = guidanceTolerance ?? TimeSpan.FromMilliseconds(180);

    public int RequiredTaps { get; } = requiredTaps >= 2
        ? requiredTaps
        : throw new ArgumentOutOfRangeException(nameof(requiredTaps));

    public int TapCount => taps.Count;

    public BeatTapStep SubmitTap(TimeSpan timestamp)
    {
        if (taps.Count >= RequiredTaps)
        {
            throw new InvalidOperationException("The beat sequence is already complete.");
        }

        var guidance = BeatTapGuidance.OnBeat;
        var intervalError = TimeSpan.Zero;

        if (taps.Count > 0)
        {
            var interval = timestamp - taps[^1];
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timestamp), "Tap timestamps must increase.");
            }

            intervalError = interval - targetInterval;
            guidance = intervalError < -tolerance
                ? BeatTapGuidance.Early
                : intervalError > tolerance
                    ? BeatTapGuidance.Late
                    : BeatTapGuidance.OnBeat;
        }

        taps.Add(timestamp);
        var isComplete = taps.Count == RequiredTaps;
        return new BeatTapStep(
            taps.Count,
            RequiredTaps,
            guidance,
            intervalError,
            isComplete,
            isComplete && IsConsistent());
    }

    private bool IsConsistent()
    {
        var errors = taps
            .Zip(taps.Skip(1), (first, second) => (second - first - targetInterval).Duration())
            .ToArray();

        return errors.Average(error => error.TotalMilliseconds) <= tolerance.TotalMilliseconds &&
               errors.Max() <= tolerance + TimeSpan.FromMilliseconds(100);
    }
}

public sealed record BeatTapStep(
    int TapCount,
    int RequiredTaps,
    BeatTapGuidance Guidance,
    TimeSpan IntervalError,
    bool IsComplete,
    bool IsSuccessful);

public enum BeatTapGuidance
{
    Early,
    OnBeat,
    Late
}
