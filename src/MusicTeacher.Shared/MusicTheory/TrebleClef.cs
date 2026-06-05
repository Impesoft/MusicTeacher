namespace MusicTeacher.Shared.MusicTheory;

public static class TrebleClef
{
    public static readonly IReadOnlyList<Pitch> BeginnerReadingNotes =
    [
        new(NoteLetter.C, 4),
        new(NoteLetter.D, 4),
        new(NoteLetter.E, 4),
        new(NoteLetter.F, 4),
        new(NoteLetter.G, 4),
        new(NoteLetter.A, 4),
        new(NoteLetter.B, 4),
        new(NoteLetter.C, 5),
        new(NoteLetter.D, 5),
        new(NoteLetter.E, 5),
        new(NoteLetter.F, 5),
        new(NoteLetter.G, 5),
        new(NoteLetter.A, 5)
    ];

    public static readonly IReadOnlyList<Pitch> BeginnerPlacementNotes =
    [
        new(NoteLetter.C, 4),
        new(NoteLetter.D, 4),
        new(NoteLetter.E, 4),
        new(NoteLetter.F, 4),
        new(NoteLetter.G, 4),
        new(NoteLetter.A, 4),
        new(NoteLetter.B, 4),
        new(NoteLetter.C, 5)
    ];

    public static readonly IReadOnlyList<Pitch> BeginnerStaffNotes = BeginnerReadingNotes;

    public static int GetStaffStep(Pitch pitch)
    {
        var octaveOffset = pitch.Octave - 4;
        var letterOffset = pitch.Letter switch
        {
            NoteLetter.C => -2,
            NoteLetter.D => -1,
            NoteLetter.E => 0,
            NoteLetter.F => 1,
            NoteLetter.G => 2,
            NoteLetter.A => 3,
            NoteLetter.B => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(pitch), pitch, null)
        };

        return octaveOffset * 7 + letterOffset;
    }

    public static Pitch GetPitchFromStaffStep(int staffStep)
    {
        var octave = 4 + Math.DivRem(staffStep + 2, 7, out var letterIndex);

        if (letterIndex < 0)
        {
            letterIndex += 7;
            octave--;
        }

        var letter = letterIndex switch
        {
            0 => NoteLetter.C,
            1 => NoteLetter.D,
            2 => NoteLetter.E,
            3 => NoteLetter.F,
            4 => NoteLetter.G,
            5 => NoteLetter.A,
            6 => NoteLetter.B,
            _ => throw new ArgumentOutOfRangeException(nameof(staffStep), staffStep, null)
        };

        return new Pitch(letter, octave);
    }
}
