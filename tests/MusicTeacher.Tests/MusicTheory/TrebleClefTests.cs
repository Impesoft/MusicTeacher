using MusicTeacher.Shared.MusicTheory;

namespace MusicTeacher.Tests.MusicTheory;

public sealed class TrebleClefTests
{
    [Theory]
    [InlineData(NoteLetter.C, 4, -2, "do")]
    [InlineData(NoteLetter.D, 4, -1, "re")]
    [InlineData(NoteLetter.E, 4, 0, "mi")]
    [InlineData(NoteLetter.F, 4, 1, "fa")]
    [InlineData(NoteLetter.G, 4, 2, "sol")]
    [InlineData(NoteLetter.A, 4, 3, "la")]
    [InlineData(NoteLetter.B, 4, 4, "ti")]
    [InlineData(NoteLetter.C, 5, 5, "do")]
    [InlineData(NoteLetter.D, 5, 6, "re")]
    [InlineData(NoteLetter.E, 5, 7, "mi")]
    [InlineData(NoteLetter.F, 5, 8, "fa")]
    [InlineData(NoteLetter.G, 5, 9, "sol")]
    [InlineData(NoteLetter.A, 5, 10, "la")]
    public void BeginnerNotesMapToTrebleStaffSteps(NoteLetter letter, int octave, int expectedStep, string expectedFixedDo)
    {
        var pitch = new Pitch(letter, octave);

        Assert.Equal(expectedStep, TrebleClef.GetStaffStep(pitch));
        Assert.Equal(expectedFixedDo, pitch.FixedDoName);
    }

    [Fact]
    public void StaffStepMappingRoundTrips()
    {
        foreach (var pitch in TrebleClef.BeginnerReadingNotes)
        {
            var staffStep = TrebleClef.GetStaffStep(pitch);

            Assert.Equal(pitch, TrebleClef.GetPitchFromStaffStep(staffStep));
        }
    }

    [Fact]
    public void BeginnerPlacementRangeRunsFromLowDoToHighDo()
    {
        Assert.Equal(new Pitch(NoteLetter.C, 4), TrebleClef.BeginnerPlacementNotes[0]);
        Assert.Equal(new Pitch(NoteLetter.C, 5), TrebleClef.BeginnerPlacementNotes[^1]);
    }

    [Fact]
    public void BeginnerReadingRangeRunsFromLowDoToHighLa()
    {
        Assert.Equal(new Pitch(NoteLetter.C, 4), TrebleClef.BeginnerReadingNotes[0]);
        Assert.Equal(new Pitch(NoteLetter.A, 5), TrebleClef.BeginnerReadingNotes[^1]);
    }
}
