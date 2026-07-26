namespace MusicTeacher.Shared.Progress;

public enum LearningSkill
{
    NoteNaming,
    StaffPlacement,
    SingleNoteEarPlay,
    HeardNotePlacement,
    MelodyEchoShort,
    MelodyEchoLong,
    StaffPhrasePitch,
    WrittenMeasureTwoFour,
    WrittenMeasureFourFour,
    SteadyBeat,
    DurationBasic,
    RhythmEcho,
    Accidentals,
    AccidentalPlacement,
    BlackKeyEarPlay
}

public sealed record LearningActivityDefinition(
    string Id,
    LearningSkill GrantsSkill,
    int RequiredStreak,
    IReadOnlyList<LearningSkill> Prerequisites);

public static class LearningCurriculum
{
    public static readonly IReadOnlyList<LearningActivityDefinition> Activities =
    [
        Activity("name-note", LearningSkill.NoteNaming, 5),
        Activity("place-note", LearningSkill.StaffPlacement, 10, LearningSkill.NoteNaming),
        Activity("hear-note-play", LearningSkill.SingleNoteEarPlay, 5, LearningSkill.StaffPlacement),

        Activity("melody-echo", LearningSkill.MelodyEchoShort, 5, LearningSkill.SingleNoteEarPlay),
        Activity("melody-echo-long", LearningSkill.MelodyEchoLong, 5, LearningSkill.MelodyEchoShort),
        Activity("staff-phrase-pitch", LearningSkill.StaffPhrasePitch, 5, LearningSkill.MelodyEchoLong),
        Activity(
            "written-measure-two-four",
            LearningSkill.WrittenMeasureTwoFour,
            3,
            LearningSkill.StaffPhrasePitch,
            LearningSkill.DurationBasic),
        Activity(
            "written-measure-four-four",
            LearningSkill.WrittenMeasureFourFour,
            3,
            LearningSkill.WrittenMeasureTwoFour,
            LearningSkill.RhythmEcho),

        Activity("beat-tap", LearningSkill.SteadyBeat, 3, LearningSkill.SingleNoteEarPlay),
        Activity("hold-duration", LearningSkill.DurationBasic, 3, LearningSkill.SteadyBeat),
        Activity("rhythm-echo", LearningSkill.RhythmEcho, 3, LearningSkill.DurationBasic),

        Activity("name-accidental", LearningSkill.Accidentals, 5, LearningSkill.StaffPlacement),
        Activity("place-accidental", LearningSkill.AccidentalPlacement, 5, LearningSkill.Accidentals),
        Activity("hear-accidental-play", LearningSkill.BlackKeyEarPlay, 5, LearningSkill.AccidentalPlacement),

        Activity(
            "hear-note-place",
            LearningSkill.HeardNotePlacement,
            0,
            LearningSkill.StaffPlacement,
            LearningSkill.SingleNoteEarPlay)
    ];

    public static LearningActivityDefinition GetActivity(string activityId)
        => Activities.FirstOrDefault(activity => activity.Id == activityId)
            ?? throw new InvalidOperationException($"Unknown learning activity '{activityId}'.");

    public static bool IsActivityUnlocked(string activityId, Func<string, int> getBestStreak)
    {
        var activity = GetActivity(activityId);
        return activity.Prerequisites.All(skill => IsSkillEarned(skill, getBestStreak));
    }

    public static bool IsActivityComplete(string activityId, Func<string, int> getBestStreak)
    {
        var activity = GetActivity(activityId);
        return activity.RequiredStreak > 0 &&
            getBestStreak(activity.Id) >= activity.RequiredStreak;
    }

    public static bool IsSkillEarned(LearningSkill skill, Func<string, int> getBestStreak)
        => Activities
            .Where(activity => IsActivityComplete(activity.Id, getBestStreak))
            .Any(activity => GrantsOrRequiresSkill(activity, skill));

    private static bool GrantsOrRequiresSkill(
        LearningActivityDefinition activity,
        LearningSkill skill)
        => activity.GrantsSkill == skill ||
            activity.Prerequisites.Any(prerequisite =>
                prerequisite == skill ||
                GrantsOrRequiresSkill(
                    Activities.First(definition => definition.GrantsSkill == prerequisite),
                    skill));

    private static LearningActivityDefinition Activity(
        string id,
        LearningSkill grantsSkill,
        int requiredStreak,
        params LearningSkill[] prerequisites)
        => new(id, grantsSkill, requiredStreak, prerequisites);
}
