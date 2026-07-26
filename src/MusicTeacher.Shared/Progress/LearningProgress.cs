namespace MusicTeacher.Shared.Progress;

public sealed record LearningProgress(
    string LessonId,
    int Attempts,
    int CorrectAnswers,
    int Streak,
    Dictionary<string, DrillLevelProgress>? DrillProgress = null,
    Dictionary<string, SongLearningProgress>? SongProgress = null)
{
    public static LearningProgress Empty(string lessonId) => new(lessonId, 0, 0, 0);
}

public sealed record DrillLevelProgress(
    int Attempts = 0,
    int CorrectAnswers = 0,
    int Streak = 0,
    int BestStreak = 0);

public sealed record SongLearningProgress(
    int PitchMeasuresCompleted = 0,
    int TimedMeasuresCompleted = 0);
