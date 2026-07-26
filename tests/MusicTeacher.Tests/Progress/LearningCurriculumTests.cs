using MusicTeacher.Shared.Progress;

namespace MusicTeacher.Tests.Progress;

public sealed class LearningCurriculumTests
{
    [Fact]
    public void CompletingSingleNoteEarPlayUnlocksMelodyAndRhythmBranches()
    {
        var streaks = new Dictionary<string, int>
        {
            ["hear-note-play"] = 5
        };

        Assert.True(IsUnlocked("melody-echo", streaks));
        Assert.True(IsUnlocked("beat-tap", streaks));
        Assert.False(IsUnlocked("melody-echo-long", streaks));
        Assert.False(IsUnlocked("hold-duration", streaks));
    }

    [Fact]
    public void StaffPhraseReadingUnlocksOnlyAfterLongMelodyEcho()
    {
        var streaks = new Dictionary<string, int>
        {
            ["melody-echo"] = 5
        };

        Assert.False(IsUnlocked("staff-phrase-pitch", streaks));

        streaks["melody-echo-long"] = 5;

        Assert.True(IsUnlocked("staff-phrase-pitch", streaks));
    }

    [Fact]
    public void TwoFourMeasureRequiresReadingAndDurationSkills()
    {
        var streaks = new Dictionary<string, int>
        {
            ["staff-phrase-pitch"] = 5
        };

        Assert.False(IsUnlocked("written-measure-two-four", streaks));

        streaks["hold-duration"] = 3;

        Assert.True(IsUnlocked("written-measure-two-four", streaks));
    }

    [Fact]
    public void FourFourMeasureRequiresPitchDurationAndRhythmBranches()
    {
        var streaks = new Dictionary<string, int>
        {
            ["written-measure-two-four"] = 3
        };

        Assert.False(IsUnlocked("written-measure-four-four", streaks));

        streaks["rhythm-echo"] = 3;

        Assert.True(IsUnlocked("written-measure-four-four", streaks));
    }

    [Fact]
    public void GuidedSongUnlocksAfterTimedFourFourMeasures()
    {
        var streaks = new Dictionary<string, int>
        {
            ["written-measure-four-four"] = 3
        };

        Assert.True(IsUnlocked("guided-song", streaks));
    }

    [Fact]
    public void AccidentalsAdvanceWithoutDependingOnMelodySkills()
    {
        var streaks = new Dictionary<string, int>
        {
            ["place-note"] = 10,
            ["name-accidental"] = 5,
            ["place-accidental"] = 5
        };

        Assert.True(IsUnlocked("name-accidental", streaks));
        Assert.True(IsUnlocked("place-accidental", streaks));
        Assert.True(IsUnlocked("hear-accidental-play", streaks));
        Assert.False(IsUnlocked("melody-echo", streaks));
    }

    [Fact]
    public void ActivityWithCombinedPrerequisitesNeedsEverySkill()
    {
        var streaks = new Dictionary<string, int>
        {
            ["place-note"] = 10
        };

        Assert.False(IsUnlocked("hear-note-place", streaks));

        streaks["hear-note-play"] = 5;

        Assert.True(IsUnlocked("hear-note-place", streaks));
    }

    [Fact]
    public void ExistingBestStreaksImmediatelyCountAsEarnedSkills()
    {
        var streaks = new Dictionary<string, int>
        {
            ["melody-echo"] = 5,
            ["beat-tap"] = 3
        };

        Assert.True(LearningCurriculum.IsSkillEarned(
            LearningSkill.MelodyEchoShort,
            id => streaks.GetValueOrDefault(id)));
        Assert.True(LearningCurriculum.IsSkillEarned(
            LearningSkill.SteadyBeat,
            id => streaks.GetValueOrDefault(id)));
    }

    [Fact]
    public void CompletedDownstreamActivityAlsoProvesItsActualPrerequisites()
    {
        var streaks = new Dictionary<string, int>
        {
            ["hold-duration"] = 3
        };

        Assert.True(LearningCurriculum.IsSkillEarned(
            LearningSkill.DurationBasic,
            id => streaks.GetValueOrDefault(id)));
        Assert.True(LearningCurriculum.IsSkillEarned(
            LearningSkill.SteadyBeat,
            id => streaks.GetValueOrDefault(id)));
        Assert.True(LearningCurriculum.IsSkillEarned(
            LearningSkill.SingleNoteEarPlay,
            id => streaks.GetValueOrDefault(id)));
        Assert.False(LearningCurriculum.IsSkillEarned(
            LearningSkill.Accidentals,
            id => streaks.GetValueOrDefault(id)));
    }

    private static bool IsUnlocked(string activityId, IReadOnlyDictionary<string, int> streaks)
        => LearningCurriculum.IsActivityUnlocked(
            activityId,
            id => streaks.GetValueOrDefault(id));
}
