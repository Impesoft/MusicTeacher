using MusicTeacher.Shared.Lessons;
using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Progress;
using System.Net.Http.Json;

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

    private static readonly IReadOnlyList<TheoryPage> TheoryPages = BuildTheoryPages();

    private readonly IReadOnlyList<int> selectableSteps = TrebleClef.BeginnerPlacementNotes
        .Select(TrebleClef.GetStaffStep)
        .ToArray();

    private LessonDefinition? lesson;
    private LearningProgress progress = LearningProgress.Empty("treble-clef-start");
    private PracticeMode practiceMode = PracticeMode.FreeExplore;
    private DrillMode mode = DrillMode.NameNote;
    private bool hasStarted;
    private Pitch currentPitch = TrebleClef.BeginnerReadingNotes[0];
    private int? selectedStep;
    private string feedbackKey = "Ready";
    private string? feedbackArgument;
    private string feedbackClass = "feedback";
    private Pitch? previousPitch;
    private int theoryPageIndex;

    private string FeedbackText => feedbackArgument is null
        ? Localizer[feedbackKey]
        : Localizer.Format(feedbackKey, feedbackArgument);

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

    private IReadOnlyList<TheoryPage> AvailableTheoryPages => TheoryPages
        .Where(page => page.Level <= CurrentAvailableTheoryLevel)
        .ToArray();

    private int CurrentAvailableTheoryLevel => 0;

    private TheoryPage CurrentTheoryPage => AvailableTheoryPages[Math.Clamp(theoryPageIndex, 0, AvailableTheoryPages.Count - 1)];

    private string CurrentTheoryTitle => GetTheoryText(CurrentTheoryPage.TitleKey);

    private string CurrentTheorySummary => GetTheoryText(CurrentTheoryPage.SummaryKey);

    private string CurrentTheoryBody => GetTheoryText(CurrentTheoryPage.BodyKey);

    private bool IsFirstTheoryPage => theoryPageIndex <= 0;

    private bool IsLastTheoryPage => theoryPageIndex >= AvailableTheoryPages.Count - 1;

    private bool IsPlacementMode => mode is DrillMode.PlaceNote or DrillMode.HearNotePlace;

    private bool IsHearingMode => mode is DrillMode.HearNotePlay or DrillMode.HearNotePlace;

    private bool ShowsStaff => mode is DrillMode.NameNote or DrillMode.PlaceNote or DrillMode.HearNotePlace;

    private bool ShowsKeyboard => mode is DrillMode.NameNote or DrillMode.PlaceNote or DrillMode.HearNotePlay;

    private bool ShowsAccidentalSelector => mode is DrillMode.PlaceNote or DrillMode.HearNotePlace;

    private Pitch? KeyboardHighlightedPitch => mode == DrillMode.PlaceNote ? currentPitch : null;

    private Pitch? DisplayedPitch => mode == DrillMode.NameNote
        ? currentPitch
        : selectedStep is null ? null : TrebleClef.GetPitchFromStaffStep(selectedStep.Value);

    private string CurrentDrillTitle => mode switch
    {
        DrillMode.NameNote => Localizer["NameTheNoteTitle"],
        DrillMode.PlaceNote => Localizer["PlaceTheNoteTitle"],
        DrillMode.HearNotePlay => Localizer["HearPlayTitle"],
        DrillMode.HearNotePlace => Localizer["HearPlaceTitle"],
        _ => throw new InvalidOperationException($"Unsupported drill mode {mode}.")
    };

    private string PromptText => mode switch
    {
        DrillMode.NameNote => Localizer["NamePrompt"],
        DrillMode.PlaceNote => Localizer.Format("PlacePrompt", GetPromptName(currentPitch)),
        DrillMode.HearNotePlay => Localizer["HearPlayPrompt"],
        DrillMode.HearNotePlace => Localizer["HearPlacePrompt"],
        _ => throw new InvalidOperationException($"Unsupported drill mode {mode}.")
    };

    protected override async Task OnInitializedAsync()
    {
        lesson = await Http.GetFromJsonAsync<LessonDefinition>("content/lessons/treble-clef-start.json");
        progress = await ProgressStore.LoadAsync("treble-clef-start");
        NextRound();
    }

    private async Task SetPracticeMode(PracticeMode nextPracticeMode)
    {
        practiceMode = nextPracticeMode;

        if (practiceMode == PracticeMode.Theory)
        {
            theoryPageIndex = 0;
            return;
        }

        if (practiceMode == PracticeMode.LearningPath && IsModeLocked(mode))
        {
            mode = GetRecommendedLearningMode();
        }

        previousPitch = null;
        NextRound();
        await PlayAssignmentNoteIfNeeded();
    }

    private async Task StartPractice(PracticeMode selectedPracticeMode)
    {
        hasStarted = true;
        await SetPracticeMode(selectedPracticeMode);
    }

    private void ReturnToStart()
    {
        hasStarted = false;
    }

    private void PreviousTheoryPage()
    {
        if (!IsFirstTheoryPage)
        {
            theoryPageIndex--;
        }
    }

    private void NextTheoryPage()
    {
        if (!IsLastTheoryPage)
        {
            theoryPageIndex++;
        }
    }

    private async Task SetMode(DrillMode nextMode)
    {
        if (IsModeLocked(nextMode))
        {
            return;
        }

        mode = nextMode;
        previousPitch = null;
        NextRound();
        await PlayAssignmentNoteIfNeeded();
    }

    private async Task SelectKeyboardPitch(Pitch pitch)
    {
        if (mode == DrillMode.PlaceNote)
        {
            await PreviewPitch(pitch);
            return;
        }

        await ChoosePitch(pitch);
    }

    private async Task ChoosePitch(Pitch pitch)
    {
        var isCorrect = pitch == currentPitch;
        await PlayClickCue(isCorrect, currentPitch);
        await RecordAnswer(isCorrect);
    }

    private async Task PreviewPitch(Pitch pitch)
    {
        await Audio.PlayNoteAsync(pitch);
    }

    private async Task PreviewStaffStep(int step)
    {
        await Audio.PlayNoteAsync(TrebleClef.GetPitchFromStaffStep(step));
    }

    private async Task SelectStaffStep(int step)
    {
        selectedStep = step;
        var isCorrect = step == TrebleClef.GetStaffStep(currentPitch);
        await PlayClickCue(isCorrect, currentPitch);
        await RecordAnswer(isCorrect);
    }

    private async Task RecordAnswer(bool isCorrect)
    {
        var wasNextModeUnlocked = GetNextMode(mode) is { } nextMode && IsModeUnlocked(nextMode);
        var updatedDrillProgress = practiceMode == PracticeMode.LearningPath
            ? UpdateCurrentDrillProgress(isCorrect)
            : progress.DrillProgress;

        var newGlobalStreak = isCorrect ? progress.Streak + 1 : 0;
        progress = progress with
        {
            Attempts = progress.Attempts + 1,
            CorrectAnswers = progress.CorrectAnswers + (isCorrect ? 1 : 0),
            Streak = newGlobalStreak,
            DrillProgress = updatedDrillProgress
        };

        feedbackKey = isCorrect ? "CorrectFeedback" : "MissedFeedback";
        feedbackArgument = null;
        feedbackClass = isCorrect ? "feedback is-correct" : "feedback is-missed";

        if (practiceMode == PracticeMode.LearningPath &&
            isCorrect &&
            GetNextMode(mode) is { } unlockedMode &&
            !wasNextModeUnlocked &&
            IsModeUnlocked(unlockedMode))
        {
            feedbackKey = "LevelUnlockedFeedback";
            feedbackArgument = Localizer[GetModeLabelKey(unlockedMode)];
        }

        await ProgressStore.SaveAsync(progress);

        if (isCorrect)
        {
            NextRound();
            await PlayAssignmentNoteIfNeeded();
        }
    }

    private async Task AdvanceRound()
    {
        NextRound();
        await PlayAssignmentNoteIfNeeded();
    }

    private void NextRound()
    {
        var notes = CurrentNotes;
        currentPitch = PickRandomPitch(notes);
        previousPitch = currentPitch;
        selectedStep = null;
        feedbackKey = mode switch
        {
            DrillMode.NameNote => "PickNameFeedback",
            DrillMode.PlaceNote => "PickStaffFeedback",
            DrillMode.HearNotePlay => "PickHeardKeyFeedback",
            DrillMode.HearNotePlace => "PickHeardStaffFeedback",
            _ => throw new InvalidOperationException($"Unsupported drill mode {mode}.")
        };
        feedbackArgument = null;
        feedbackClass = "feedback";
    }

    private Pitch PickRandomPitch(IReadOnlyList<Pitch> notes)
    {
        if (notes.Count == 0)
        {
            throw new InvalidOperationException("A drill needs at least one note.");
        }

        if (notes.Count == 1 || previousPitch is null)
        {
            return notes[Random.Shared.Next(notes.Count)];
        }

        var candidates = notes.Where(note => note != previousPitch.Value).ToArray();
        return candidates[Random.Shared.Next(candidates.Length)];
    }

    private IReadOnlyList<Pitch> CurrentNotes => mode is DrillMode.NameNote or DrillMode.HearNotePlay
        ? TrebleClef.BeginnerReadingNotes
        : TrebleClef.BeginnerPlacementNotes;

    private string GetModeClass(DrillMode drillMode)
    {
        var classes = new List<string> { "mode-button" };

        if (mode == drillMode)
        {
            classes.Add("is-active");
        }

        if (IsModeLocked(drillMode))
        {
            classes.Add("is-locked");
        }

        return string.Join(' ', classes);
    }

    private string GetPracticeModeClass(PracticeMode candidate)
        => practiceMode == candidate ? "practice-mode-button is-active" : "practice-mode-button";

    private string GetCultureClass(string cultureName)
        => string.Equals(Localizer.CurrentCulture.Name, cultureName, StringComparison.OrdinalIgnoreCase)
            ? "language-button is-active"
            : "language-button";

    private async Task ChangeCulture(string cultureName)
    {
        await Localizer.SetCultureAsync(cultureName);
        await InvokeAsync(StateHasChanged);
    }

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

    private async Task PlayClickCue(bool isCorrect, Pitch pitch)
    {
        if (isCorrect)
        {
            if (!IsHearingMode)
            {
                await Audio.PlayNoteAsync(pitch);
            }

            return;
        }

        await Audio.PlayBuzzerAsync();
    }

    private async Task PlayAssignmentNote()
    {
        await Audio.PlayNoteAsync(currentPitch);
    }

    private async Task PlayAssignmentNoteIfNeeded()
    {
        if (IsHearingMode)
        {
            await PlayAssignmentNote();
        }
    }

    private string GetPromptName(Pitch pitch)
        => $"{(pitch.Octave == 4 ? Localizer["LowOctave"] : Localizer["HighOctave"])} {pitch.FixedDoName}";

    private string GetTheoryText(string resourceKey)
        => CurrentTheoryPage.Pitch is { } pitch
            ? Localizer.Format(resourceKey, GetPromptName(pitch), pitch.ScientificName.ToLowerInvariant())
            : Localizer[resourceKey];

    private static IReadOnlyList<TheoryPage> BuildTheoryPages()
    {
        var pages = new List<TheoryPage>
        {
            new(0, "TheoryStaffTitle", "TheoryStaffSummary", "TheoryStaffBody", TheoryVisual.Staff),
            new(0, "TheoryTrebleClefTitle", "TheoryTrebleClefSummary", "TheoryTrebleClefBody", TheoryVisual.TrebleClef)
        };

        pages.AddRange(TrebleClef.BeginnerStaffNotes.Select(pitch =>
            new TheoryPage(0, "TheorySingleNoteTitle", "TheorySingleNoteSummary", "TheorySingleNoteBody", TheoryVisual.SingleNote, pitch)));

        return pages;
    }

    private enum DrillMode
    {
        NameNote,
        PlaceNote,
        HearNotePlay,
        HearNotePlace
    }

    private enum PracticeMode
    {
        FreeExplore,
        LearningPath,
        Theory
    }

    private sealed record LearningLevel(DrillMode Mode, int RequiredStreak);

    private sealed record TheoryPage(int Level, string TitleKey, string SummaryKey, string BodyKey, TheoryVisual Visual, Pitch? Pitch = null);

    private enum TheoryVisual
    {
        Staff,
        TrebleClef,
        SingleNote
    }
}
