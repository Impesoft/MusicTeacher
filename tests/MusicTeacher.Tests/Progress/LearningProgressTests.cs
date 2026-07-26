using System.Text.Json;
using MusicTeacher.Shared.Progress;

namespace MusicTeacher.Tests.Progress;

public sealed class LearningProgressTests
{
    [Fact]
    public void ExistingSongCheckpointsDefaultToUnfinishedJoinedPerformances()
    {
        var checkpoint = new SongLearningProgress(
            PitchMeasuresCompleted: 4,
            TimedMeasuresCompleted: 4);

        Assert.Equal(0, checkpoint.SectionsCompleted);
        Assert.False(checkpoint.CompleteSongCompleted);
    }

    [Fact]
    public void OldSavedProgressWithoutDrillProgressStillDeserializes()
    {
        const string json = """
            {
              "LessonId": "treble-clef-start",
              "Attempts": 3,
              "CorrectAnswers": 2,
              "Streak": 1
            }
            """;

        var progress = JsonSerializer.Deserialize<LearningProgress>(json);

        Assert.NotNull(progress);
        Assert.Equal("treble-clef-start", progress.LessonId);
        Assert.Equal(3, progress.Attempts);
        Assert.Equal(2, progress.CorrectAnswers);
        Assert.Equal(1, progress.Streak);
        Assert.Null(progress.DrillProgress);
    }

    [Fact]
    public void DrillProgressStoresIndependentLevelStreaks()
    {
        var progress = LearningProgress.Empty("treble-clef-start") with
        {
            DrillProgress = new()
            {
                ["name-note"] = new DrillLevelProgress(Attempts: 7, CorrectAnswers: 5, Streak: 5, BestStreak: 5),
                ["place-note"] = new DrillLevelProgress(Attempts: 3, CorrectAnswers: 2, Streak: 2, BestStreak: 2)
            }
        };

        var json = JsonSerializer.Serialize(progress);
        var restored = JsonSerializer.Deserialize<LearningProgress>(json);

        Assert.NotNull(restored?.DrillProgress);
        Assert.Equal(5, restored.DrillProgress["name-note"].BestStreak);
        Assert.Equal(2, restored.DrillProgress["place-note"].BestStreak);
    }
}
