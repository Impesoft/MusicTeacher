using MusicTeacher.Shared.Progress;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private static readonly IReadOnlyList<LearningLevel> LearningLevels =
    [
        new(DrillMode.NameNote, 5),
        new(DrillMode.PlaceNote, 10),
        new(DrillMode.HearNotePlay, 5),
        new(DrillMode.HearNotePlace, 0)
    ];

    private DrillLevelProgress CurrentLevelProgress => GetLevelProgress(mode);

    private int DisplayAttempts => practiceMode == PracticeMode.LearningPath
        ? CurrentLevelProgress.Attempts
        : progress.Attempts;

    private int DisplayCorrectAnswers => practiceMode == PracticeMode.LearningPath
        ? CurrentLevelProgress.CorrectAnswers
        : progress.CorrectAnswers;

    private int DisplayStreak => practiceMode == PracticeMode.LearningPath
        ? CurrentLevelProgress.Streak
        : progress.Streak;

    private int CurrentLevelNumber => LearningLevels
        .Select((level, index) => new { level, index })
        .FirstOrDefault(item => item.level.Mode == mode)?.index + 1 ?? 1;

    private LearningLevel CurrentLearningLevel => LearningLevels.First(level => level.Mode == mode);

    private string LearningGoalText => CurrentLearningLevel.RequiredStreak == 0
        ? Localizer["FinalLevelGoal"]
        : Localizer.Format(
            "LearningGoal",
            CurrentLevelProgress.BestStreak,
            CurrentLearningLevel.RequiredStreak,
            Localizer[GetModeLabelKey(GetNextMode(mode) ?? mode)]);

    private async Task ResetProgress()
    {
        var confirmed = await JS.InvokeAsync<bool>("confirm", [Localizer["ResetProgressConfirm"]]);
        if (!confirmed)
        {
            return;
        }

        progress = LearningProgress.Empty(progress.LessonId);
        await ProgressStore.ResetAsync(progress.LessonId);

        if (practiceMode == PracticeMode.LearningPath)
        {
            mode = GetRecommendedLearningMode();
        }

        previousPitch = null;
        NextRound();
        feedbackKey = "ProgressResetFeedback";
        feedbackArgument = null;
        feedbackClass = "feedback";
        await PlayAssignmentNoteIfNeeded();
    }

    private Dictionary<string, DrillLevelProgress> UpdateCurrentDrillProgress(bool isCorrect)
    {
        var drillProgress = GetDrillProgress();
        var current = GetLevelProgress(mode);
        var streak = isCorrect ? current.Streak + 1 : 0;
        drillProgress[GetModeKey(mode)] = current with
        {
            Attempts = current.Attempts + 1,
            CorrectAnswers = current.CorrectAnswers + (isCorrect ? 1 : 0),
            Streak = streak,
            BestStreak = Math.Max(current.BestStreak, streak)
        };

        return drillProgress;
    }

    private bool IsModeLocked(DrillMode drillMode)
        => practiceMode == PracticeMode.LearningPath && !IsModeUnlocked(drillMode);

    private bool IsModeUnlocked(DrillMode drillMode)
        => drillMode switch
        {
            DrillMode.NameNote => true,
            DrillMode.PlaceNote => GetLevelProgress(DrillMode.NameNote).BestStreak >= 5,
            DrillMode.HearNotePlay => GetLevelProgress(DrillMode.PlaceNote).BestStreak >= 10,
            DrillMode.HearNotePlace => GetLevelProgress(DrillMode.HearNotePlay).BestStreak >= 5,
            _ => false
        };

    private DrillMode GetRecommendedLearningMode()
        => LearningLevels.FirstOrDefault(level => IsModeUnlocked(level.Mode) && !IsLevelComplete(level.Mode))?.Mode
            ?? LearningLevels.Last(level => IsModeUnlocked(level.Mode)).Mode;

    private bool IsLevelComplete(DrillMode drillMode)
    {
        var requiredStreak = LearningLevels.First(level => level.Mode == drillMode).RequiredStreak;
        return requiredStreak > 0 && GetLevelProgress(drillMode).BestStreak >= requiredStreak;
    }

    private DrillMode? GetNextMode(DrillMode drillMode)
    {
        var index = LearningLevels
            .Select((level, levelIndex) => new { level, levelIndex })
            .FirstOrDefault(item => item.level.Mode == drillMode)?.levelIndex;

        return index is null || index.Value >= LearningLevels.Count - 1
            ? null
            : LearningLevels[index.Value + 1].Mode;
    }

    private DrillLevelProgress GetLevelProgress(DrillMode drillMode)
    {
        var drillProgress = progress.DrillProgress;
        return drillProgress is not null && drillProgress.TryGetValue(GetModeKey(drillMode), out var levelProgress)
            ? levelProgress
            : new DrillLevelProgress();
    }

    private Dictionary<string, DrillLevelProgress> GetDrillProgress()
        => progress.DrillProgress is null
            ? []
            : new Dictionary<string, DrillLevelProgress>(progress.DrillProgress);

    private static string GetModeKey(DrillMode drillMode)
        => drillMode switch
        {
            DrillMode.NameNote => "name-note",
            DrillMode.PlaceNote => "place-note",
            DrillMode.HearNotePlay => "hear-note-play",
            DrillMode.HearNotePlace => "hear-note-place",
            _ => throw new InvalidOperationException($"Unsupported drill mode {drillMode}.")
        };

    private static string GetModeLabelKey(DrillMode drillMode)
        => drillMode switch
        {
            DrillMode.NameNote => "NameMode",
            DrillMode.PlaceNote => "PlaceMode",
            DrillMode.HearNotePlay => "HearPlayMode",
            DrillMode.HearNotePlace => "HearPlaceMode",
            _ => throw new InvalidOperationException($"Unsupported drill mode {drillMode}.")
        };

    private sealed record LearningLevel(DrillMode Mode, int RequiredStreak);
}
