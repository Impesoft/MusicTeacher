using MusicTeacher.Shared.Practice;

namespace MusicTeacher.Tests.Practice;

public sealed class PitchSequenceEvaluatorTests
{
    [Fact]
    public void CorrectNotesAdvanceUntilComplete()
    {
        var evaluator = new PitchSequenceEvaluator([60, 62, 64]);

        Assert.Equal(PitchSequenceStepResult.Correct, evaluator.SubmitNote(60).Result);
        Assert.Equal(PitchSequenceStepResult.Correct, evaluator.SubmitNote(62).Result);
        var finalStep = evaluator.SubmitNote(64);

        Assert.Equal(PitchSequenceStepResult.Completed, finalStep.Result);
        Assert.Equal(3, finalStep.Position);
        Assert.True(evaluator.IsComplete);
    }

    [Fact]
    public void IncorrectNoteRecordsMistakeWithoutAdvancing()
    {
        var evaluator = new PitchSequenceEvaluator([60, 62]);

        var step = evaluator.SubmitNote(61);

        Assert.Equal(PitchSequenceStepResult.Incorrect, step.Result);
        Assert.Equal(0, evaluator.Position);
        Assert.Equal(1, evaluator.Mistakes);
    }

    [Fact]
    public void LearnerCanContinueAfterMistake()
    {
        var evaluator = new PitchSequenceEvaluator([60, 62]);

        evaluator.SubmitNote(61);
        evaluator.SubmitNote(60);
        var finalStep = evaluator.SubmitNote(62);

        Assert.Equal(PitchSequenceStepResult.Completed, finalStep.Result);
        Assert.Equal(1, evaluator.Mistakes);
    }

    [Fact]
    public void NotesAfterCompletionDoNotChangeAttempt()
    {
        var evaluator = new PitchSequenceEvaluator([60]);
        evaluator.SubmitNote(60);

        var extraStep = evaluator.SubmitNote(62);

        Assert.Equal(PitchSequenceStepResult.AlreadyComplete, extraStep.Result);
        Assert.Equal(1, evaluator.Position);
        Assert.Equal(0, evaluator.Mistakes);
    }

    [Fact]
    public void ResetStartsNewAttempt()
    {
        var evaluator = new PitchSequenceEvaluator([60, 62]);
        evaluator.SubmitNote(61);
        evaluator.SubmitNote(60);

        evaluator.Reset();

        Assert.Equal(0, evaluator.Position);
        Assert.Equal(0, evaluator.Mistakes);
        Assert.False(evaluator.IsComplete);
    }

    [Fact]
    public void EmptySequenceIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new PitchSequenceEvaluator([]));
    }

    [Fact]
    public void SequenceIsCopiedAtConstruction()
    {
        var notes = new List<int> { 60, 62 };
        var evaluator = new PitchSequenceEvaluator(notes);

        notes[0] = 72;

        Assert.Equal(PitchSequenceStepResult.Correct, evaluator.SubmitNote(60).Result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(128)]
    public void InvalidMidiNotesAreRejected(int midiNote)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PitchSequenceEvaluator([midiNote]));
    }
}
