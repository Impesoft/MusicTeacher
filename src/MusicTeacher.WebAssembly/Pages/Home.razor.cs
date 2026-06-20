using MusicTeacher.Shared.Lessons;
using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Progress;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private const string NoteNameModeStorageKey = "music-teacher-note-name-mode";
    private const string VisualThemeStorageKey = "music-teacher-visual-theme";
    private const string AvatarStorageKey = "music-teacher-avatar";
    private const string EarnedBadgesStorageKey = "music-teacher-earned-badges";

    private static readonly IReadOnlyList<VisualThemeOption> VisualThemeOptions =
    [
        new("anime-sky", "VisualThemeAnimeSky"),
        new("manga-night", "VisualThemeMangaNight"),
        new("candy-stage", "VisualThemeCandyStage"),
        new("starship", "VisualThemeStarship"),
        new("forest-band", "VisualThemeForestBand"),
        new("city-pop", "VisualThemeCityPop"),
        new("sticker-wall", "VisualThemeStickerWall"),
        new("ocean-show", "VisualThemeOceanShow")
    ];

    private static readonly IReadOnlyList<AvatarOption> AvatarOptions =
    [
        new("melody", "AvatarMelody"),
        new("mika", "AvatarMika"),
        new("nova", "AvatarNova"),
        new("pixel", "AvatarPixel"),
        new("luna", "AvatarLuna"),
        new("kai", "AvatarKai"),
        new("riko", "AvatarRiko"),
        new("zara", "AvatarZara"),
        new("tempo", "AvatarTempo"),
        new("sora", "AvatarSora"),
        new("nori", "AvatarNori"),
        new("aria", "AvatarAria")
    ];

    private static readonly IReadOnlyList<BadgeAward> BadgeAwards =
    [
        new(DrillMode.PlaceNote, "place-notes", "BadgePlaceNotesTitle", "BadgePlaceNotesDescription", "◆"),
        new(DrillMode.NameAccidental, "accidentals", "BadgeAccidentalsTitle", "BadgeAccidentalsDescription", "♯"),
        new(DrillMode.PlaceAccidental, "place-accidentals", "BadgePlaceAccidentalsTitle", "BadgePlaceAccidentalsDescription", "♭"),
        new(DrillMode.HearNotePlay, "ear-training", "BadgeEarTrainingTitle", "BadgeEarTrainingDescription", "♪"),
        new(DrillMode.HearAccidentalPlay, "black-keys", "BadgeBlackKeysTitle", "BadgeBlackKeysDescription", "●"),
        new(DrillMode.HearNotePlace, "staff-master", "BadgeStaffMasterTitle", "BadgeStaffMasterDescription", "★")
    ];

    private LessonDefinition? lesson;
    private LearningProgress progress = LearningProgress.Empty("treble-clef-start");
    private PracticeMode practiceMode = PracticeMode.FreeExplore;
    private DrillMode mode = DrillMode.NameNote;
    private NoteNameMode noteNameMode = NoteNameMode.FixedDo;
    private string selectedVisualTheme = "anime-sky";
    private string selectedAvatar = "melody";
    private bool hasStarted;
    private Pitch currentPitch = TrebleClef.BeginnerReadingNotes[0];
    private int? selectedStep;
    private Accidental selectedAccidental = Accidental.Natural;
    private string feedbackKey = "Ready";
    private string? feedbackArgument;
    private string feedbackClass = "feedback";
    private UnlockToastViewModel? unlockToast;
    private HashSet<string> earnedBadgeIds = [];
    private Pitch? previousPitch;
    private int theoryPageIndex;
    private bool isCustomizerOpen = false;
    private CustomizerTab activeCustomizerTab = CustomizerTab.Avatar;

    protected override async Task OnInitializedAsync()
    {
        lesson = await Http.GetFromJsonAsync<LessonDefinition>("content/lessons/treble-clef-start.json");
        progress = await ProgressStore.LoadAsync("treble-clef-start");
        noteNameMode = ParseNoteNameMode(await JS.InvokeAsync<string?>("localStorage.getItem", [NoteNameModeStorageKey]));
        selectedVisualTheme = ResolveVisualTheme(await JS.InvokeAsync<string?>("localStorage.getItem", [VisualThemeStorageKey]));
        selectedAvatar = ResolveAvatar(await JS.InvokeAsync<string?>("localStorage.getItem", [AvatarStorageKey]));
        earnedBadgeIds = ParseEarnedBadges(await JS.InvokeAsync<string?>("localStorage.getItem", [EarnedBadgesStorageKey]));
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

    private void OpenCustomizer()
    {
        activeCustomizerTab = CustomizerTab.Avatar;
        isCustomizerOpen = true;
    }

    private void CloseCustomizer()
    {
        isCustomizerOpen = false;
    }

    private void SetCustomizerTab(CustomizerTab tab)
    {
        activeCustomizerTab = tab;
    }

    private string GetTabClass(CustomizerTab tab)
        => activeCustomizerTab == tab ? "customizer-tab-button is-active" : "customizer-tab-button";

    private void DismissUnlockToast()
    {
        unlockToast = null;
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

    private IReadOnlyList<BadgeAward> EarnedBadges => BadgeAwards
        .Where(badge => earnedBadgeIds.Contains(badge.Id))
        .ToArray();

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
        => VisualThemeOptions.Any(option => option.Value == visualTheme) ? visualTheme! : "anime-sky";

    private static string ResolveAvatar(string? avatar)
        => AvatarOptions.Any(option => option.Value == avatar) ? avatar! : "melody";

    private static HashSet<string> ParseEarnedBadges(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<HashSet<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task SaveEarnedBadges()
    {
        await JS.InvokeVoidAsync("localStorage.setItem", EarnedBadgesStorageKey, JsonSerializer.Serialize(earnedBadgeIds));
    }

    private enum DrillMode
    {
        NameNote,
        PlaceNote,
        NameAccidental,
        PlaceAccidental,
        HearNotePlay,
        HearAccidentalPlay,
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

    private enum CustomizerTab
    {
        Avatar,
        Theme,
        Settings
    }

    private sealed record VisualThemeOption(string Value, string LabelKey);

    private sealed record AvatarOption(string Value, string LabelKey);

    private sealed record BadgeAward(DrillMode Mode, string Id, string TitleKey, string DescriptionKey, string Icon);

    private sealed record UnlockToastViewModel(string Message, string BadgeTitle, string BadgeDescription, string BadgeIcon);
}
