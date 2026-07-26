using MusicTeacher.Shared.Practice;

namespace MusicTeacher.Tests.Practice;

public sealed class TimedWrittenMeasureEvaluatorTests
{
    private static readonly TimeSpan Beat = TimeSpan.FromMilliseconds(600);

    [Fact]
    public void FourNotesOnTheirBeatsAndHeldOneBeatCompletePerfectly()
    {
        var evaluator = CreateEvaluator();

        for (var index = 0; index < 4; index++)
        {
            Assert.Equal(
                TimedMeasureStartResult.OnBeat,
                evaluator.StartNote(60 + index, Beat * (index + 1)).Result);
            var end = evaluator.EndNote(60 + index, Beat);
            Assert.Equal(TimedMeasureEndResult.OnTarget, end.Result);
        }

        Assert.True(evaluator.IsComplete);
        Assert.Equal(0, evaluator.Mistakes);
    }

    [Fact]
    public void EarlyLateAndWrongDurationAreRecordedButDoNotDerailTheMeasure()
    {
        var evaluator = CreateEvaluator();

        Assert.Equal(TimedMeasureStartResult.Early, evaluator.StartNote(60, TimeSpan.FromMilliseconds(300)).Result);
        Assert.Equal(TimedMeasureEndResult.TooShort, evaluator.EndNote(60, TimeSpan.FromMilliseconds(200)).Result);
        Assert.Equal(TimedMeasureStartResult.Late, evaluator.StartNote(61, TimeSpan.FromMilliseconds(1_500)).Result);
        evaluator.EndNote(61, Beat);

        Assert.Equal(3, evaluator.Mistakes);
        Assert.Equal(2, evaluator.Position);
    }

    [Fact]
    public void WrongPitchDoesNotAdvance()
    {
        var evaluator = CreateEvaluator();

        Assert.Equal(TimedMeasureStartResult.IncorrectPitch, evaluator.StartNote(72, Beat).Result);

        Assert.Equal(0, evaluator.Position);
        Assert.Equal(1, evaluator.Mistakes);
    }

    [Fact]
    public void BeginnerMouseTimingAllowsTimeToReleaseAndTravelToTheNextKey()
    {
        var oneSecondBeat = TimeSpan.FromSeconds(1);
        var evaluator = new TimedWrittenMeasureEvaluator(
            [
                new(60, 1),
                new(62, 1)
            ],
            oneSecondBeat,
            TimeSpan.FromMilliseconds(450),
            TimeSpan.FromMilliseconds(600));

        Assert.Equal(
            TimedMeasureStartResult.OnBeat,
            evaluator.StartNote(60, TimeSpan.FromMilliseconds(1_350)).Result);
        Assert.Equal(
            TimedMeasureEndResult.OnTarget,
            evaluator.EndNote(60, TimeSpan.FromMilliseconds(450)).Result);
        Assert.Equal(
            TimedMeasureStartResult.OnBeat,
            evaluator.StartNote(62, TimeSpan.FromMilliseconds(2_250)).Result);
        Assert.Equal(
            TimedMeasureEndResult.OnTarget,
            evaluator.EndNote(62, TimeSpan.FromMilliseconds(500)).Result);
        Assert.Equal(0, evaluator.Mistakes);
    }

    private static TimedWrittenMeasureEvaluator CreateEvaluator()
        => new(
            [
                new(60, 1),
                new(61, 1),
                new(62, 1),
                new(63, 1)
            ],
            Beat);
}
