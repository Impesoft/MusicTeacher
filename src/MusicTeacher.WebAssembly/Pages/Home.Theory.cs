using MusicTeacher.Shared.MusicTheory;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private static readonly IReadOnlyList<TheoryPage> TheoryPages = BuildTheoryPages();

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

    private sealed record TheoryPage(int Level, string TitleKey, string SummaryKey, string BodyKey, TheoryVisual Visual, Pitch? Pitch = null);

    private enum TheoryVisual
    {
        Staff,
        TrebleClef,
        SingleNote
    }
}
