using MusicTeacher.Shared.Progress;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
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

    private int CurrentLevelNumber => LearningCurriculum.Activities
        .Select((activity, index) => new { activity, index })
        .FirstOrDefault(item => item.activity.Id == GetModeKey(mode))?.index + 1 ?? 1;

    private LearningActivityDefinition CurrentLearningActivity
        => LearningCurriculum.GetActivity(GetModeKey(mode));

    private string LearningGoalText => CurrentLearningActivity.RequiredStreak == 0
        ? Localizer["FinalLevelGoal"]
        : mode is DrillMode.BeatTap or DrillMode.HoldDuration or DrillMode.RhythmEcho
            ? Localizer.Format(
                mode switch
                {
                    DrillMode.BeatTap => "BeatTapLearningGoal",
                    DrillMode.HoldDuration => "DurationHoldLearningGoal",
                    _ => "RhythmEchoLearningGoal"
                },
                CurrentLevelProgress.BestStreak,
                CurrentLearningActivity.RequiredStreak)
        : Localizer.Format(
            "LearningGoal",
            CurrentLevelProgress.BestStreak,
            CurrentLearningActivity.RequiredStreak,
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
        feedbackArguments = [];
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
        => LearningCurriculum.IsActivityUnlocked(GetModeKey(drillMode), GetBestStreak);

    private DrillMode GetRecommendedLearningMode()
        => LearningCurriculum.Activities
            .Where(activity => IsModeUnlocked(GetDrillMode(activity.Id)))
            .FirstOrDefault(activity => !IsLevelComplete(GetDrillMode(activity.Id))) is { } nextActivity
                ? GetDrillMode(nextActivity.Id)
                : GetDrillMode(LearningCurriculum.Activities.Last(
                    activity => IsModeUnlocked(GetDrillMode(activity.Id))).Id);

    private bool IsLevelComplete(DrillMode drillMode)
        => LearningCurriculum.IsActivityComplete(GetModeKey(drillMode), GetBestStreak);

    private DrillMode? GetNextMode(DrillMode drillMode)
    {
        if (drillMode == DrillMode.BeatTap)
        {
            return DrillMode.HoldDuration;
        }

        if (drillMode == DrillMode.HoldDuration)
        {
            return DrillMode.RhythmEcho;
        }

        if (drillMode == DrillMode.RhythmEcho)
        {
            return null;
        }

        return drillMode switch
        {
            DrillMode.NameNote => DrillMode.PlaceNote,
            DrillMode.PlaceNote => DrillMode.HearNotePlay,
            DrillMode.HearNotePlay => DrillMode.MelodyEcho,
            DrillMode.MelodyEcho => DrillMode.MelodyEchoLong,
            DrillMode.NameAccidental => DrillMode.PlaceAccidental,
            DrillMode.PlaceAccidental => DrillMode.HearAccidentalPlay,
            _ => null
        };
    }

    private int GetBestStreak(string activityId)
        => GetLevelProgress(GetDrillMode(activityId)).BestStreak;

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
            DrillMode.NameAccidental => "name-accidental",
            DrillMode.PlaceAccidental => "place-accidental",
            DrillMode.HearNotePlay => "hear-note-play",
            DrillMode.BeatTap => "beat-tap",
            DrillMode.HoldDuration => "hold-duration",
            DrillMode.RhythmEcho => "rhythm-echo",
            DrillMode.MelodyEcho => "melody-echo",
            DrillMode.MelodyEchoLong => "melody-echo-long",
            DrillMode.HearAccidentalPlay => "hear-accidental-play",
            DrillMode.HearNotePlace => "hear-note-place",
            _ => throw new InvalidOperationException($"Unsupported drill mode {drillMode}.")
        };

    private static string GetModeLabelKey(DrillMode drillMode)
        => drillMode switch
        {
            DrillMode.NameNote => "NameMode",
            DrillMode.PlaceNote => "PlaceMode",
            DrillMode.NameAccidental => "NameAccidentalMode",
            DrillMode.PlaceAccidental => "PlaceAccidentalMode",
            DrillMode.HearNotePlay => "HearPlayMode",
            DrillMode.BeatTap => "BeatTapMode",
            DrillMode.HoldDuration => "DurationHoldMode",
            DrillMode.RhythmEcho => "RhythmEchoMode",
            DrillMode.MelodyEcho => "MelodyEchoMode",
            DrillMode.MelodyEchoLong => "MelodyEchoLongMode",
            DrillMode.HearAccidentalPlay => "HearAccidentalPlayMode",
            DrillMode.HearNotePlace => "HearPlaceMode",
            _ => throw new InvalidOperationException($"Unsupported drill mode {drillMode}.")
        };

    private static DrillMode GetDrillMode(string activityId)
        => activityId switch
        {
            "name-note" => DrillMode.NameNote,
            "place-note" => DrillMode.PlaceNote,
            "name-accidental" => DrillMode.NameAccidental,
            "place-accidental" => DrillMode.PlaceAccidental,
            "hear-note-play" => DrillMode.HearNotePlay,
            "beat-tap" => DrillMode.BeatTap,
            "hold-duration" => DrillMode.HoldDuration,
            "rhythm-echo" => DrillMode.RhythmEcho,
            "melody-echo" => DrillMode.MelodyEcho,
            "melody-echo-long" => DrillMode.MelodyEchoLong,
            "hear-accidental-play" => DrillMode.HearAccidentalPlay,
            "hear-note-place" => DrillMode.HearNotePlace,
            _ => throw new InvalidOperationException($"Unsupported activity '{activityId}'.")
        };
}
