using MusicTeacher.Shared.Practice;

namespace MusicTeacher.Tests.Practice;

public sealed class RhythmPatternEvaluatorTests
{
    [Fact]
    public void ExactSoundBeatsCompleteSuccessfully()
    {
        var evaluator = new RhythmPatternEvaluator([0, 2], TimeSpan.FromMilliseconds(600));

        var first = evaluator.SubmitTap(TimeSpan.Zero);
        var second = evaluator.SubmitTap(TimeSpan.FromMilliseconds(1_200));

        Assert.Equal(RhythmTapGuidance.OnBeat, first.Guidance);
        Assert.True(second.IsComplete);
        Assert.True(second.IsSuccessful);
    }

    [Theory]
    [InlineData(300, RhythmTapGuidance.Early)]
    [InlineData(900, RhythmTapGuidance.Late)]
    public void ReportsDirectionAroundAnExpectedBeat(int milliseconds, RhythmTapGuidance guidance)
    {
        var evaluator = new RhythmPatternEvaluator([1], TimeSpan.FromMilliseconds(600));

        var result = evaluator.SubmitTap(TimeSpan.FromMilliseconds(milliseconds));

        Assert.Equal(guidance, result.Guidance);
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void ToleranceBoundariesAreAccepted()
    {
        var early = new RhythmPatternEvaluator([1], TimeSpan.FromMilliseconds(600));
        var late = new RhythmPatternEvaluator([1], TimeSpan.FromMilliseconds(600));

        Assert.True(early.SubmitTap(TimeSpan.FromMilliseconds(410)).IsSuccessful);
        Assert.True(late.SubmitTap(TimeSpan.FromMilliseconds(790)).IsSuccessful);
    }
}
