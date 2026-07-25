using MusicTeacher.Shared.MusicTheory;

namespace MusicTeacher.Tests.MusicTheory;

public sealed class PitchTests
{
    [Theory]
    [InlineData(NoteLetter.C, 4, Accidental.Natural, 60)]
    [InlineData(NoteLetter.C, 4, Accidental.Sharp, 61)]
    [InlineData(NoteLetter.D, 4, Accidental.Flat, 61)]
    [InlineData(NoteLetter.A, 4, Accidental.Natural, 69)]
    public void MidiNoteUsesScientificPitch(NoteLetter letter, int octave, Accidental accidental, int expected)
    {
        Assert.Equal(expected, new Pitch(letter, octave, accidental).MidiNote);
    }

    [Theory]
    [InlineData(NoteLetter.A, 4, Accidental.Natural, 440.0)]
    [InlineData(NoteLetter.C, 4, Accidental.Natural, 261.63)]
    [InlineData(NoteLetter.C, 4, Accidental.Sharp, 277.18)]
    [InlineData(NoteLetter.D, 4, Accidental.Flat, 277.18)]
    [InlineData(NoteLetter.C, 5, Accidental.Natural, 523.25)]
    public void FrequencyUsesEqualTemperament(NoteLetter letter, int octave, Accidental accidental, double expectedFrequency)
    {
        var pitch = new Pitch(letter, octave, accidental);

        Assert.Equal(expectedFrequency, pitch.FrequencyHz, precision: 2);
    }

    [Fact]
    public void AccidentalsExposeScientificAndDisplayNames()
    {
        var sharp = new Pitch(NoteLetter.C, 4, Accidental.Sharp);
        var flat = new Pitch(NoteLetter.D, 4, Accidental.Flat);

        Assert.Equal("C#4", sharp.ScientificName);
        Assert.Equal("c♯4", sharp.DisplayName);
        Assert.Equal("do♯", sharp.FixedDoName);
        Assert.Equal("Db4", flat.ScientificName);
        Assert.Equal("d♭4", flat.DisplayName);
        Assert.Equal("re♭", flat.FixedDoName);
    }

    [Theory]
    [InlineData(NoteLetter.A, 4, Accidental.Sharp, NoteLetter.B, 4, Accidental.Flat, true)]
    [InlineData(NoteLetter.C, 4, Accidental.Sharp, NoteLetter.D, 4, Accidental.Flat, true)]
    [InlineData(NoteLetter.F, 4, Accidental.Sharp, NoteLetter.G, 4, Accidental.Flat, true)]
    [InlineData(NoteLetter.C, 4, Accidental.Natural, NoteLetter.D, 4, Accidental.Natural, false)]
    public void IsEnharmonicEquivalentToMatchesMidiNotes(
        NoteLetter letter1, int octave1, Accidental accidental1,
        NoteLetter letter2, int octave2, Accidental accidental2,
        bool expected)
    {
        var pitch1 = new Pitch(letter1, octave1, accidental1);
        var pitch2 = new Pitch(letter2, octave2, accidental2);

        Assert.Equal(expected, pitch1.IsEnharmonicEquivalentTo(pitch2));
    }
}
