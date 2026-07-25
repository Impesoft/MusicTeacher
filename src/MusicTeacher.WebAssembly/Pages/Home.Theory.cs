using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Progress;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private static readonly IReadOnlyList<TheoryPage> TheoryPages = BuildTheoryPages();

    private IReadOnlyList<TheoryPage> AvailableTheoryPages => TheoryPages
        .Where(page => page.Prerequisites.All(IsSkillEarned))
        .ToArray();

    private bool IsSkillEarned(LearningSkill skill)
        => LearningCurriculum.IsSkillEarned(skill, GetBestStreak);

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
            new("TheoryStaffTitle", "TheoryStaffSummary", "TheoryStaffBody", TheoryVisual.Staff),
            new("TheoryTrebleClefTitle", "TheoryTrebleClefSummary", "TheoryTrebleClefBody", TheoryVisual.TrebleClef)
        };

        pages.AddRange(TrebleClef.BeginnerStaffNotes.Select(pitch =>
            new TheoryPage("TheorySingleNoteTitle", "TheorySingleNoteSummary", "TheorySingleNoteBody", TheoryVisual.SingleNote, pitch)));

        pages.Add(new TheoryPage("TheoryAccidentalsTitle", "TheoryAccidentalsSummary", "TheoryAccidentalsBody", TheoryVisual.SingleNote, new Pitch(NoteLetter.C, 4, Accidental.Sharp), LearningSkill.StaffPlacement));
        pages.Add(new TheoryPage("TheoryBlackKeysTitle", "TheoryBlackKeysSummary", "TheoryBlackKeysBody", TheoryVisual.Keyboard, null, LearningSkill.StaffPlacement));
        pages.Add(new TheoryPage("TheorySteadyBeatTitle", "TheorySteadyBeatSummary", "TheorySteadyBeatBody", TheoryVisual.Beat, null, LearningSkill.SingleNoteEarPlay));
        pages.Add(new TheoryPage("TheorySoundLengthTitle", "TheorySoundLengthSummary", "TheorySoundLengthBody", TheoryVisual.DurationContrast, null, LearningSkill.SteadyBeat));
        pages.Add(new TheoryPage("TheoryBeatDurationsTitle", "TheoryBeatDurationsSummary", "TheoryBeatDurationsBody", TheoryVisual.BeatDurations, null, LearningSkill.SteadyBeat));
        pages.Add(new TheoryPage("TheoryNoteValuesTitle", "TheoryNoteValuesSummary", "TheoryNoteValuesBody", TheoryVisual.NoteValues, null, LearningSkill.SteadyBeat));
        pages.Add(new TheoryPage("TheoryRestsTitle", "TheoryRestsSummary", "TheoryRestsBody", TheoryVisual.Rests, null, LearningSkill.DurationBasic));

        return pages;
    }

    private sealed record TheoryPage(
        string TitleKey,
        string SummaryKey,
        string BodyKey,
        TheoryVisual Visual,
        Pitch? Pitch = null,
        params LearningSkill[] Prerequisites);

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
