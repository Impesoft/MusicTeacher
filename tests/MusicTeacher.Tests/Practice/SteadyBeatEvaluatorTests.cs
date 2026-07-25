using MusicTeacher.Shared.Practice;

namespace MusicTeacher.Tests.Practice;

public sealed class SteadyBeatEvaluatorTests
{
    [Fact]
    public void EvenTapsCompleteSuccessfully()
    {
        var evaluator = new SteadyBeatEvaluator(TimeSpan.FromMilliseconds(600), requiredTaps: 4);

        var steps = Enumerable.Range(0, 4)
            .Select(index => evaluator.SubmitTap(TimeSpan.FromMilliseconds(index * 600)))
            .ToArray();

        Assert.All(steps, step => Assert.Equal(BeatTapGuidance.OnBeat, step.Guidance));
        Assert.True(steps[^1].IsComplete);
        Assert.True(steps[^1].IsSuccessful);
    }

    [Theory]
    [InlineData(350, BeatTapGuidance.Early)]
    [InlineData(850, BeatTapGuidance.Late)]
    public void GivesGentleDirectionForAnUnevenInterval(int secondTapMilliseconds, BeatTapGuidance expected)
    {
        var evaluator = new SteadyBeatEvaluator(TimeSpan.FromMilliseconds(600));
        evaluator.SubmitTap(TimeSpan.Zero);

        var step = evaluator.SubmitTap(TimeSpan.FromMilliseconds(secondTapMilliseconds));

        Assert.Equal(expected, step.Guidance);
    }

    [Fact]
    public void BroadlyConsistentTapsAreAccepted()
    {
        var evaluator = new SteadyBeatEvaluator(TimeSpan.FromMilliseconds(600), requiredTaps: 5);
        int[] tapTimes = [0, 510, 1_170, 1_700, 2_350];

        BeatTapStep? finalStep = null;
        foreach (var milliseconds in tapTimes)
        {
            finalStep = evaluator.SubmitTap(TimeSpan.FromMilliseconds(milliseconds));
        }

        Assert.True(finalStep!.IsSuccessful);
    }

    [Fact]
    public void VeryUnevenTapsDoNotPass()
    {
        var evaluator = new SteadyBeatEvaluator(TimeSpan.FromMilliseconds(600), requiredTaps: 5);
        int[] tapTimes = [0, 250, 1_200, 1_450, 2_400];

        BeatTapStep? finalStep = null;
        foreach (var milliseconds in tapTimes)
        {
            finalStep = evaluator.SubmitTap(TimeSpan.FromMilliseconds(milliseconds));
        }

        Assert.True(finalStep!.IsComplete);
        Assert.False(finalStep.IsSuccessful);
    }
}
