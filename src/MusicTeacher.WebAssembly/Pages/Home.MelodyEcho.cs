using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Practice;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private static readonly IReadOnlyList<IReadOnlyList<Pitch>> BeginnerMelodyPhrases =
    [
        [new(NoteLetter.C, 4), new(NoteLetter.D, 4)],
        [new(NoteLetter.E, 4), new(NoteLetter.D, 4)],
        [new(NoteLetter.C, 4), new(NoteLetter.E, 4)],
        [new(NoteLetter.G, 4), new(NoteLetter.A, 4)],
        [new(NoteLetter.C, 4), new(NoteLetter.D, 4), new(NoteLetter.E, 4)],
        [new(NoteLetter.E, 4), new(NoteLetter.D, 4), new(NoteLetter.C, 4)],
        [new(NoteLetter.G, 4), new(NoteLetter.A, 4), new(NoteLetter.B, 4)],
        [new(NoteLetter.C, 5), new(NoteLetter.G, 4), new(NoteLetter.C, 5)]
    ];

    private static readonly IReadOnlyList<IReadOnlyList<Pitch>> LongerMelodyPhrases =
    [
        [new(NoteLetter.C, 4), new(NoteLetter.D, 4), new(NoteLetter.E, 4), new(NoteLetter.D, 4)],
        [new(NoteLetter.E, 4), new(NoteLetter.F, 4), new(NoteLetter.G, 4), new(NoteLetter.E, 4)],
        [new(NoteLetter.G, 4), new(NoteLetter.A, 4), new(NoteLetter.B, 4), new(NoteLetter.G, 4)],
        [new(NoteLetter.C, 5), new(NoteLetter.B, 4), new(NoteLetter.A, 4), new(NoteLetter.G, 4)],
        [new(NoteLetter.C, 4), new(NoteLetter.E, 4), new(NoteLetter.G, 4), new(NoteLetter.E, 4), new(NoteLetter.C, 4)],
        [new(NoteLetter.G, 4), new(NoteLetter.A, 4), new(NoteLetter.G, 4), new(NoteLetter.E, 4), new(NoteLetter.C, 4)],
        [new(NoteLetter.C, 5), new(NoteLetter.G, 4), new(NoteLetter.A, 4), new(NoteLetter.B, 4), new(NoteLetter.C, 5)],
        [new(NoteLetter.E, 4), new(NoteLetter.G, 4), new(NoteLetter.F, 4), new(NoteLetter.D, 4), new(NoteLetter.C, 4)]
    ];

    private PitchSequenceEvaluator? melodyEvaluator;
    private IReadOnlyList<Pitch> melodyPhrase = [];
    private IReadOnlyList<Pitch>? previousMelodyPhrase;
    private int? teacherDemonstratedMidiNote;
    private bool isDemonstratingMelody;
    private int melodyDemonstrationVersion;

    private bool IsMelodyEchoMode => mode is DrillMode.MelodyEcho or DrillMode.MelodyEchoLong;
    private bool KeyboardInputDisabled
        => IsMelodyEchoMode && isDemonstratingMelody ||
            IsGuidedSongMode && IsGuidedSongComplete;
    private int MelodyPosition => melodyEvaluator?.Position ?? 0;
    private int MelodyLength => melodyEvaluator?.ExpectedMidiNotes.Count ?? 0;

    private void StartNewMelodyPhrase()
    {
        var phrasePool = mode == DrillMode.MelodyEchoLong ? LongerMelodyPhrases : BeginnerMelodyPhrases;
        var candidates = phrasePool
            .Where(phrase => previousMelodyPhrase is null || !phrase.SequenceEqual(previousMelodyPhrase))
            .ToArray();

        melodyPhrase = candidates[Random.Shared.Next(candidates.Length)];
        previousMelodyPhrase = melodyPhrase;
        melodyEvaluator = new PitchSequenceEvaluator(melodyPhrase.Select(pitch => pitch.MidiNote).ToArray());
        teacherDemonstratedMidiNote = null;
        isDemonstratingMelody = false;
        feedbackKey = "MelodyEchoListenFeedback";
        feedbackArguments = [];
        feedbackClass = "feedback";
    }

    private async Task DemonstrateMelodyPhrase()
    {
        if (!IsMelodyEchoMode || melodyPhrase.Count == 0)
        {
            return;
        }

        var version = ++melodyDemonstrationVersion;
        isDemonstratingMelody = true;
        feedbackKey = "MelodyEchoListenFeedback";
        feedbackArguments = [];
        feedbackClass = "feedback";

        foreach (var pitch in melodyPhrase)
        {
            if (version != melodyDemonstrationVersion || !IsMelodyEchoMode)
            {
                return;
            }

            teacherDemonstratedMidiNote = pitch.MidiNote;
            await InvokeAsync(StateHasChanged);
            await Audio.PlayNoteAsync(pitch);
            await Task.Delay(550);
            teacherDemonstratedMidiNote = null;
            await InvokeAsync(StateHasChanged);
            await Task.Delay(160);
        }

        if (version != melodyDemonstrationVersion || !IsMelodyEchoMode)
        {
            return;
        }

        isDemonstratingMelody = false;
        feedbackKey = "MelodyEchoYourTurnFeedback";
        await InvokeAsync(StateHasChanged);
    }

    private async Task SubmitMelodyNote(int midiNote)
    {
        if (isDemonstratingMelody || melodyEvaluator is null)
        {
            return;
        }

        var step = melodyEvaluator.SubmitNote(midiNote);
        switch (step.Result)
        {
            case PitchSequenceStepResult.Incorrect:
                feedbackKey = "MelodyEchoTryAgainFeedback";
                feedbackArguments = [step.Position + 1];
                feedbackClass = "feedback is-missed";
                await Audio.PlayBuzzerAsync();
                break;

            case PitchSequenceStepResult.Correct:
                feedbackKey = "MelodyEchoProgressFeedback";
                feedbackArguments = [step.Position, MelodyLength];
                feedbackClass = "feedback is-correct";
                break;

            case PitchSequenceStepResult.Completed:
                await CompleteMelodyPhrase(step.Mistakes == 0);
                break;
        }
    }

    private async Task CompleteMelodyPhrase(bool isPerfect)
    {
        var wasNextModeUnlocked = GetNextMode(mode) is { } nextMode && IsModeUnlocked(nextMode);
        var updatedDrillProgress = practiceMode == PracticeMode.LearningPath
            ? UpdateCurrentDrillProgress(isPerfect)
            : progress.DrillProgress;

        progress = progress with
        {
            Attempts = progress.Attempts + 1,
            CorrectAnswers = progress.CorrectAnswers + (isPerfect ? 1 : 0),
            Streak = isPerfect ? progress.Streak + 1 : 0,
            DrillProgress = updatedDrillProgress
        };

        feedbackKey = isPerfect ? "MelodyEchoCompleteFeedback" : "MelodyEchoCompleteWithMistakesFeedback";
        feedbackArguments = [];
        feedbackClass = "feedback is-correct";

        if (practiceMode == PracticeMode.LearningPath &&
            isPerfect &&
            GetNextMode(mode) is { } unlockedMode &&
            !wasNextModeUnlocked &&
            IsModeUnlocked(unlockedMode))
        {
            feedbackKey = "LevelUnlockedFeedback";
            feedbackArguments = [Localizer[GetModeLabelKey(unlockedMode)]];
            await AwardUnlock(unlockedMode);
        }

        await ProgressStore.SaveAsync(progress);
        await InvokeAsync(StateHasChanged);
        await Task.Delay(850);

        StartNewMelodyPhrase();
        await DemonstrateMelodyPhrase();
    }
}
