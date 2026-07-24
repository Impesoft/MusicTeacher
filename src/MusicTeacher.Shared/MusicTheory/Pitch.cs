namespace MusicTeacher.Shared.MusicTheory;

public readonly record struct Pitch(NoteLetter Letter, int Octave, Accidental Accidental = Accidental.Natural)
{
    public string ScientificName => $"{Letter}{ScientificAccidental}{Octave}";

    public int MidiNote => (Octave + 1) * 12 + SemitoneFromC + (int)Accidental;

    public double FrequencyHz
    {
        get
        {
            return 440d * Math.Pow(2d, (MidiNote - 69) / 12d);
        }
    }

    public string FixedDoName => $"{BaseFixedDoName}{DisplayAccidental}";

    public string DisplayName => $"{Letter}{DisplayAccidental}{Octave}".ToLowerInvariant();

    private string BaseFixedDoName => Letter switch
    {
        NoteLetter.C => "do",
        NoteLetter.D => "re",
        NoteLetter.E => "mi",
        NoteLetter.F => "fa",
        NoteLetter.G => "sol",
        NoteLetter.A => "la",
        NoteLetter.B => "ti",
        _ => throw new ArgumentOutOfRangeException(nameof(Letter), Letter, null)
    };

    private string DisplayAccidental => Accidental switch
    {
        Accidental.Flat => "♭",
        Accidental.Natural => string.Empty,
        Accidental.Sharp => "♯",
        _ => throw new ArgumentOutOfRangeException(nameof(Accidental), Accidental, null)
    };

    private string ScientificAccidental => Accidental switch
    {
        Accidental.Flat => "b",
        Accidental.Natural => string.Empty,
        Accidental.Sharp => "#",
        _ => throw new ArgumentOutOfRangeException(nameof(Accidental), Accidental, null)
    };

    private int SemitoneFromC => Letter switch
    {
        NoteLetter.C => 0,
        NoteLetter.D => 2,
        NoteLetter.E => 4,
        NoteLetter.F => 5,
        NoteLetter.G => 7,
        NoteLetter.A => 9,
        NoteLetter.B => 11,
        _ => throw new ArgumentOutOfRangeException(nameof(Letter), Letter, null)
    };
}
