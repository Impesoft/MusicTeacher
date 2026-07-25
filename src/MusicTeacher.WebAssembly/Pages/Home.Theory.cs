using MusicTeacher.Shared.MusicTheory;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private static readonly IReadOnlyList<TheoryPage> TheoryPages = BuildTheoryPages();

    private IReadOnlyList<TheoryPage> AvailableTheoryPages => TheoryPages
        .Where(page => page.Level <= CurrentAvailableTheoryLevel)
        .ToArray();

    private int CurrentAvailableTheoryLevel
        => IsLevelComplete(DrillMode.HoldDuration)
            ? 4
            : IsLevelComplete(DrillMode.BeatTap)
            ? 3
            : IsLevelComplete(DrillMode.HearNotePlay)
            ? 2
            : IsModeUnlocked(DrillMode.NameAccidental)
                ? 1
                : 0;

    private TheoryPage CurrentTheoryPage => AvailableTheoryPages[Math.Clamp(theoryPageIndex, 0, AvailableTheoryPages.Count - 1)];

    private string CurrentTheoryTitle => GetTheoryText(CurrentTheoryPage.TitleKey);

    private string CurrentTheorySummary => GetTheoryText(CurrentTheoryPage.SummaryKey);

    private string CurrentTheoryBody => GetTheoryText(CurrentTheoryPage.BodyKey);

    private bool IsFirstTheoryPage => theoryPageIndex <= 0;

    private bool IsLastTheoryPage => theoryPageIndex >= AvailableTheoryPages.Count - 1;

    private int theoryPlaybackVersion;
    private int activeRestExampleBeat;

    private void PreviousTheoryPage()
    {
        if (!IsFirstTheoryPage)
        {
            CancelTheoryPlayback();
            theoryPageIndex--;
        }
    }

    private void NextTheoryPage()
    {
        if (!IsLastTheoryPage)
        {
            CancelTheoryPlayback();
            theoryPageIndex++;
        }
    }

    private async Task PlayTheoryDuration(int beats)
        => await Audio.PlayDurationExampleAsync(beats);

    private async Task PlayTheoryRestPattern()
    {
        var version = ++theoryPlaybackVersion;

        for (var beat = 1; beat <= 4; beat++)
        {
            if (version != theoryPlaybackVersion || practiceMode != PracticeMode.Theory)
            {
                return;
            }

            activeRestExampleBeat = beat;
            await Audio.PlayMidiNoteAsync(84);
            if (beat is 1 or 3)
            {
                await Audio.PlayDurationExampleAsync(1);
            }
            await InvokeAsync(StateHasChanged);
            await Task.Delay(BeatInterval);
        }

        if (version == theoryPlaybackVersion)
        {
            activeRestExampleBeat = 0;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void CancelTheoryPlayback()
    {
        theoryPlaybackVersion++;
        activeRestExampleBeat = 0;
    }

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

        pages.Add(new TheoryPage(1, "TheoryAccidentalsTitle", "TheoryAccidentalsSummary", "TheoryAccidentalsBody", TheoryVisual.SingleNote, new Pitch(NoteLetter.C, 4, Accidental.Sharp)));
        pages.Add(new TheoryPage(1, "TheoryBlackKeysTitle", "TheoryBlackKeysSummary", "TheoryBlackKeysBody", TheoryVisual.Keyboard));
        pages.Add(new TheoryPage(2, "TheorySteadyBeatTitle", "TheorySteadyBeatSummary", "TheorySteadyBeatBody", TheoryVisual.Beat));
        pages.Add(new TheoryPage(3, "TheorySoundLengthTitle", "TheorySoundLengthSummary", "TheorySoundLengthBody", TheoryVisual.DurationContrast));
        pages.Add(new TheoryPage(3, "TheoryBeatDurationsTitle", "TheoryBeatDurationsSummary", "TheoryBeatDurationsBody", TheoryVisual.BeatDurations));
        pages.Add(new TheoryPage(3, "TheoryNoteValuesTitle", "TheoryNoteValuesSummary", "TheoryNoteValuesBody", TheoryVisual.NoteValues));
        pages.Add(new TheoryPage(4, "TheoryRestsTitle", "TheoryRestsSummary", "TheoryRestsBody", TheoryVisual.Rests));

        return pages;
    }

    private sealed record TheoryPage(int Level, string TitleKey, string SummaryKey, string BodyKey, TheoryVisual Visual, Pitch? Pitch = null);

    private enum TheoryVisual
    {
        Staff,
        TrebleClef,
        SingleNote,
        Keyboard,
        Beat,
        DurationContrast,
        BeatDurations,
        NoteValues,
        Rests
    }
}
