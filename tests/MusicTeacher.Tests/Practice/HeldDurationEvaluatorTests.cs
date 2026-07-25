using MusicTeacher.Shared.Practice;

namespace MusicTeacher.Tests.Practice;

public sealed class HeldDurationEvaluatorTests
{
    [Theory]
    [InlineData(1, 600)]
    [InlineData(2, 1_200)]
    [InlineData(4, 2_400)]
    public void ExactBeatDurationsAreAccepted(int beats, int milliseconds)
    {
        var result = HeldDurationEvaluator.Evaluate(beats, TimeSpan.FromMilliseconds(milliseconds));

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void AcceptanceBoundariesAreInclusive()
    {
        var example = HeldDurationEvaluator.Evaluate(2, TimeSpan.FromMilliseconds(1_200));

        Assert.True(HeldDurationEvaluator.Evaluate(2, example.MinimumAcceptedDuration).IsSuccessful);
        Assert.True(HeldDurationEvaluator.Evaluate(2, example.MaximumAcceptedDuration).IsSuccessful);
    }

    [Fact]
    public void ReleasesBeforeTheBandAreTooShort()
    {
        var example = HeldDurationEvaluator.Evaluate(4, TimeSpan.FromMilliseconds(2_400));

        var result = HeldDurationEvaluator.Evaluate(
            4,
            example.MinimumAcceptedDuration - TimeSpan.FromMilliseconds(1));

        Assert.Equal(HeldDurationGuidance.TooShort, result.Guidance);
    }

    [Fact]
    public void ReleasesAfterTheBandAreTooLong()
    {
        var example = HeldDurationEvaluator.Evaluate(1, TimeSpan.FromMilliseconds(600));

        var result = HeldDurationEvaluator.Evaluate(
            1,
            example.MaximumAcceptedDuration + TimeSpan.FromMilliseconds(1));

        Assert.Equal(HeldDurationGuidance.TooLong, result.Guidance);
    }
}
