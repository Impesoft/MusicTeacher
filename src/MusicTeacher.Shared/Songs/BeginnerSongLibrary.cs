using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Practice;
using MusicTeacher.Shared.Progress;

namespace MusicTeacher.Shared.Songs;

public static class BeginnerSongLibrary
{
    private static readonly Pitch C4 = new(NoteLetter.C, 4);
    private static readonly Pitch D4 = new(NoteLetter.D, 4);
    private static readonly Pitch E4 = new(NoteLetter.E, 4);
    private static readonly Pitch F4 = new(NoteLetter.F, 4);
    private static readonly Pitch G4 = new(NoteLetter.G, 4);

    public static readonly SongDefinition LevelComplete = new(
        "level-complete",
        "SongLevelCompleteTitle",
        100,
        [LearningSkill.WrittenMeasureFourFour],
        [
            new(
                "launch",
                "SongSectionLaunch",
                [
                    Measure("launch-1", C4, D4, E4, G4),
                    Measure("launch-2", G4, E4, D4, C4)
                ]),
            new(
                "victory",
                "SongSectionVictory",
                [
                    Measure("victory-1", E4, F4, G4, E4),
                    Measure("victory-2", D4, E4, C4, C4)
                ])
        ]);

    private static SongMeasure Measure(string id, params Pitch[] pitches)
        => new(
            id,
            new WrittenMeasure(
                TimeSignature.FourFour,
                pitches.Select(pitch => new WrittenMeasureNote(pitch, 1)).ToArray()));
}
