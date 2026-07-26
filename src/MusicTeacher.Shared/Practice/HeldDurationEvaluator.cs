namespace MusicTeacher.Shared.Practice;

public static class HeldDurationEvaluator
{
    public static HeldDurationResult Evaluate(
        int requestedBeats,
        TimeSpan heldDuration,
        TimeSpan? beatInterval = null,
        TimeSpan? acceptedTolerance = null)
    {
        if (requestedBeats is not (1 or 2 or 4))
        {
            throw new ArgumentOutOfRangeException(nameof(requestedBeats));
        }

        var interval = beatInterval ?? TimeSpan.FromMilliseconds(600);
        var target = interval * requestedBeats;
        var tolerance = acceptedTolerance ??
            TimeSpan.FromMilliseconds(Math.Min(350, 140 + requestedBeats * 70));
        var minimum = target - tolerance;
        var maximum = target + tolerance;
        var guidance = heldDuration < minimum
            ? HeldDurationGuidance.TooShort
            : heldDuration > maximum
                ? HeldDurationGuidance.TooLong
                : HeldDurationGuidance.OnTarget;

        return new HeldDurationResult(
            requestedBeats,
            heldDuration,
            target,
            minimum,
            maximum,
            guidance);
    }
}

public sealed record HeldDurationResult(
    int RequestedBeats,
    TimeSpan HeldDuration,
    TimeSpan TargetDuration,
    TimeSpan MinimumAcceptedDuration,
    TimeSpan MaximumAcceptedDuration,
    HeldDurationGuidance Guidance)
{
    public bool IsSuccessful => Guidance == HeldDurationGuidance.OnTarget;
}

public enum HeldDurationGuidance
{
    TooShort,
    OnTarget,
    TooLong
}
