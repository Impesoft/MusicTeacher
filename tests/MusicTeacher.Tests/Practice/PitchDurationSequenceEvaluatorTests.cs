using MusicTeacher.Shared.Practice;

namespace MusicTeacher.Tests.Practice;

public sealed class PitchDurationSequenceEvaluatorTests
{
    private static readonly TimeSpan Beat = TimeSpan.FromMilliseconds(600);

    [Fact]
    public void CorrectPitchesHeldForOneBeatCompleteMeasure()
    {
        var evaluator = new PitchDurationSequenceEvaluator(
            [new(60, 1), new(62, 1)],
            Beat);

        Assert.Equal(PitchDurationStartResult.Started, evaluator.StartNote(60));
        Assert.Equal(PitchDurationStepResult.Correct, evaluator.EndNote(60, Beat).Result);

        // No gap or absolute timestamp is submitted, so the learner may pause here.
        Assert.Equal(PitchDurationStartResult.Started, evaluator.StartNote(62));
        var completed = evaluator.EndNote(62, Beat);

        Assert.Equal(PitchDurationStepResult.Completed, completed.Result);
        Assert.True(evaluator.IsComplete);
        Assert.Equal(0, evaluator.Mistakes);
    }

    [Fact]
    public void IncorrectPitchDoesNotStartOrAdvanceNote()
    {
        var evaluator = new PitchDurationSequenceEvaluator([new(60, 1)], Beat);

        Assert.Equal(PitchDurationStartResult.IncorrectPitch, evaluator.StartNote(62));
        Assert.Equal(0, evaluator.Position);
        Assert.Equal(1, evaluator.Mistakes);
    }

    [Fact]
    public void IncorrectDurationKeepsSameWrittenNoteForRetry()
    {
        var evaluator = new PitchDurationSequenceEvaluator([new(60, 1)], Beat);

        evaluator.StartNote(60);
        var tooShort = evaluator.EndNote(60, TimeSpan.FromMilliseconds(300));
        evaluator.StartNote(60);
        var retry = evaluator.EndNote(60, Beat);

        Assert.Equal(PitchDurationStepResult.TooShort, tooShort.Result);
        Assert.Equal(PitchDurationStepResult.Completed, retry.Result);
        Assert.Equal(1, evaluator.Mistakes);
    }
}
