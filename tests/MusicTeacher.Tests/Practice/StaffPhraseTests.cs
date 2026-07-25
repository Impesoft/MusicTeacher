using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Practice;

namespace MusicTeacher.Tests.Practice;

public sealed class StaffPhraseTests
{
    [Fact]
    public void CreatesPitchOnlyEvaluatorInWrittenOrder()
    {
        var phrase = new StaffPhrase(
        [
            new Pitch(NoteLetter.C, 4),
            new Pitch(NoteLetter.E, 4),
            new Pitch(NoteLetter.D, 4)
        ]);
        var evaluator = phrase.CreatePitchEvaluator();

        Assert.Equal([60, 64, 62], evaluator.ExpectedMidiNotes);
        Assert.Equal(PitchSequenceStepResult.Correct, evaluator.SubmitNote(60).Result);
        Assert.Equal(PitchSequenceStepResult.Correct, evaluator.SubmitNote(64).Result);
        Assert.Equal(PitchSequenceStepResult.Completed, evaluator.SubmitNote(62).Result);
    }

    [Fact]
    public void ModelCannotAccidentallyIntroduceTimingBeforeItIsTaught()
    {
        var propertyNames = typeof(StaffPhrase)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["Pitches"], propertyNames);
        Assert.DoesNotContain(propertyNames, name =>
            name.Contains("Duration", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Tempo", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Time", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RequiresAtLeastTwoWrittenNotes()
    {
        Assert.Throws<ArgumentException>(() =>
            new StaffPhrase([new Pitch(NoteLetter.C, 4)]));
    }
}
