using MusicTeacher.Shared.Lessons;
using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Progress;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private const string NoteNameModeStorageKey = "music-teacher-note-name-mode";
    private const string VisualThemeStorageKey = "music-teacher-visual-theme";
    private const string AvatarStorageKey = "music-teacher-avatar";

    private static readonly IReadOnlyList<VisualThemeOption> VisualThemeOptions =
    [
        new("classic", "VisualThemeClassic", "background: linear-gradient(135deg, #8ed4e8, #ffc857);"),
        new("rainbow", "VisualThemeRainbow", "background: linear-gradient(135deg, #ff6f91, #ffd166, #59d9a5);"),
        new("space", "VisualThemeSpace", "background: linear-gradient(135deg, #243b6b, #7b61ff, #26d9d0);")
    ];

    private static readonly IReadOnlyList<AvatarOption> AvatarOptions =
    [
        new("star", "AvatarStar", "★"),
        new("rocket", "AvatarRocket", "♬"),
        new("spark", "AvatarSpark", "✦")
    ];

    private LessonDefinition? lesson;
    private LearningProgress progress = LearningProgress.Empty("treble-clef-start");
    private PracticeMode practiceMode = PracticeMode.FreeExplore;
    private DrillMode mode = DrillMode.NameNote;
    private NoteNameMode noteNameMode = NoteNameMode.FixedDo;
    private string selectedVisualTheme = "classic";
    private string selectedAvatar = "star";
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
        noteNameMode = ParseNoteNameMode(await JS.InvokeAsync<string?>("localStorage.getItem", [NoteNameModeStorageKey]));
        selectedVisualTheme = ResolveVisualTheme(await JS.InvokeAsync<string?>("localStorage.getItem", [VisualThemeStorageKey]));
        selectedAvatar = ResolveAvatar(await JS.InvokeAsync<string?>("localStorage.getItem", [AvatarStorageKey]));
        await ApplyVisualTheme();
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

    private bool UseAlphabeticalNoteNames => noteNameMode == NoteNameMode.Alphabetical;

    private string SelectedAvatarIcon => AvatarOptions.First(option => option.Value == selectedAvatar).Icon;

    private async Task SetNoteNameMode(NoteNameMode nextMode)
    {
        noteNameMode = nextMode;
        await JS.InvokeVoidAsync("localStorage.setItem", NoteNameModeStorageKey, GetNoteNameModeStorageValue(nextMode));
    }

    private string GetNoteNameModeClass(NoteNameMode option)
        => noteNameMode == option ? "choice-button is-active" : "choice-button";

    private async Task SetVisualTheme(string visualTheme)
    {
        selectedVisualTheme = ResolveVisualTheme(visualTheme);
        await JS.InvokeVoidAsync("localStorage.setItem", VisualThemeStorageKey, selectedVisualTheme);
        await ApplyVisualTheme();
    }

    private async Task ApplyVisualTheme()
        => await JS.InvokeVoidAsync("musicTeacherProfile.setVisualTheme", selectedVisualTheme);

    private string GetVisualThemeClass(string visualTheme)
        => selectedVisualTheme == visualTheme ? "theme-button is-active" : "theme-button";

    private async Task SetAvatar(string avatar)
    {
        selectedAvatar = ResolveAvatar(avatar);
        await JS.InvokeVoidAsync("localStorage.setItem", AvatarStorageKey, selectedAvatar);
    }

    private string GetAvatarClass(string avatar)
        => selectedAvatar == avatar ? "avatar-button is-active" : "avatar-button";

    private static NoteNameMode ParseNoteNameMode(string? storedMode)
        => string.Equals(storedMode, "alphabetical", StringComparison.OrdinalIgnoreCase)
            ? NoteNameMode.Alphabetical
            : NoteNameMode.FixedDo;

    private static string GetNoteNameModeStorageValue(NoteNameMode mode)
        => mode == NoteNameMode.Alphabetical ? "alphabetical" : "fixed-do";

    private static string ResolveVisualTheme(string? visualTheme)
        => VisualThemeOptions.Any(option => option.Value == visualTheme) ? visualTheme! : "classic";

    private static string ResolveAvatar(string? avatar)
        => AvatarOptions.Any(option => option.Value == avatar) ? avatar! : "star";

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

    private enum NoteNameMode
    {
        FixedDo,
        Alphabetical
    }

    private sealed record VisualThemeOption(string Value, string LabelKey, string SwatchStyle);

    private sealed record AvatarOption(string Value, string LabelKey, string Icon);
}
