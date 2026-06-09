using MusicTeacher.Shared.Lessons;
using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Progress;
using System.Net.Http.Json;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
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

    private string GetCultureClass(string cultureName)
        => string.Equals(Localizer.CurrentCulture.Name, cultureName, StringComparison.OrdinalIgnoreCase)
            ? "language-button is-active"
            : "language-button";

    private async Task ChangeCulture(string cultureName)
    {
        await Localizer.SetCultureAsync(cultureName);
        await InvokeAsync(StateHasChanged);
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

}
