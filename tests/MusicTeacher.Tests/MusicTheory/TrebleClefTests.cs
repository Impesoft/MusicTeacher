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
    [InlineData(NoteLetter.B, 5, 11, "ti")]
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
    public void BeginnerPlacementRangeMatchesReadingRange()
    {
        Assert.Equal(new Pitch(NoteLetter.C, 4), TrebleClef.BeginnerPlacementNotes[0]);
        Assert.Equal(new Pitch(NoteLetter.B, 5), TrebleClef.BeginnerPlacementNotes[^1]);
        Assert.Equal(TrebleClef.BeginnerReadingNotes, TrebleClef.BeginnerPlacementNotes);
    }

    [Fact]
    public void BeginnerReadingRangeRunsFromLowDoToHighTi()
    {
        Assert.Equal(new Pitch(NoteLetter.C, 4), TrebleClef.BeginnerReadingNotes[0]);
        Assert.Equal(new Pitch(NoteLetter.B, 5), TrebleClef.BeginnerReadingNotes[^1]);
    }

    [Fact]
    public void BeginnerAccidentalRangeUsesBlackKeyEnharmonics()
    {
        Assert.Equal(20, TrebleClef.BeginnerAccidentalNotes.Count);
        Assert.Contains(new Pitch(NoteLetter.C, 4, Accidental.Sharp), TrebleClef.BeginnerAccidentalNotes);
        Assert.Contains(new Pitch(NoteLetter.D, 4, Accidental.Flat), TrebleClef.BeginnerAccidentalNotes);
        Assert.Contains(new Pitch(NoteLetter.A, 5, Accidental.Sharp), TrebleClef.BeginnerAccidentalNotes);
        Assert.Contains(new Pitch(NoteLetter.B, 5, Accidental.Flat), TrebleClef.BeginnerAccidentalNotes);
        Assert.DoesNotContain(new Pitch(NoteLetter.E, 4, Accidental.Sharp), TrebleClef.BeginnerAccidentalNotes);
        Assert.DoesNotContain(new Pitch(NoteLetter.C, 4, Accidental.Flat), TrebleClef.BeginnerAccidentalNotes);
    }

    [Fact]
    public void AccidentalsKeepTheirNaturalStaffStep()
    {
        var cSharp = new Pitch(NoteLetter.C, 4, Accidental.Sharp);
        var dFlat = new Pitch(NoteLetter.D, 4, Accidental.Flat);

        Assert.Equal(TrebleClef.GetStaffStep(new Pitch(NoteLetter.C, 4)), TrebleClef.GetStaffStep(cSharp));
        Assert.Equal(TrebleClef.GetStaffStep(new Pitch(NoteLetter.D, 4)), TrebleClef.GetStaffStep(dFlat));
    }

    [Theory]
    [InlineData(NoteLetter.B, NoteLetter.B, 4, NoteLetter.B, 4)]
    [InlineData(NoteLetter.B, NoteLetter.C, 5, NoteLetter.B, 5)]
    [InlineData(NoteLetter.B, NoteLetter.A, 5, NoteLetter.B, 5)]
    [InlineData(NoteLetter.C, NoteLetter.A, 5, NoteLetter.C, 5)]
    public void SolfegeButtonPreviewKeepsTheAnswerRowInOneRegister(
        NoteLetter requestedLetter,
        NoteLetter referenceLetter,
        int referenceOctave,
        NoteLetter expectedLetter,
        int expectedOctave)
    {
        var referencePitch = new Pitch(referenceLetter, referenceOctave);

        Assert.Equal(
            new Pitch(expectedLetter, expectedOctave),
            TrebleClef.GetSolfegeButtonPreviewPitch(requestedLetter, referencePitch));
    }
}
