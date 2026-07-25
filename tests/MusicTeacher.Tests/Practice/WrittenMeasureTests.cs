using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Practice;

namespace MusicTeacher.Tests.Practice;

public sealed class WrittenMeasureTests
{
    [Fact]
    public void TwoQuarterNotesExactlyFillTwoFour()
    {
        var measure = new WrittenMeasure(
            TimeSignature.TwoFour,
            [
                new(new Pitch(NoteLetter.C, 4), 1),
                new(new Pitch(NoteLetter.D, 4), 1)
            ]);

        Assert.Equal(TimeSignature.TwoFour, measure.TimeSignature);
        Assert.Equal(2, measure.Notes.Sum(note => note.DurationBeats));
    }

    [Fact]
    public void RejectsNotesThatDoNotFillMeasure()
    {
        Assert.Throws<ArgumentException>(() =>
            new WrittenMeasure(
                TimeSignature.TwoFour,
                [new(new Pitch(NoteLetter.C, 4), 1)]));
    }
}
