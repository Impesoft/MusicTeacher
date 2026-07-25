using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Practice;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private static readonly IReadOnlyList<StaffPhrase> BeginnerStaffPhrases =
    [
        new([new(NoteLetter.C, 4), new(NoteLetter.D, 4)]),
        new([new(NoteLetter.E, 4), new(NoteLetter.C, 4)]),
        new([new(NoteLetter.G, 4), new(NoteLetter.A, 4)]),
        new([new(NoteLetter.C, 5), new(NoteLetter.B, 4)]),
        new([new(NoteLetter.C, 4), new(NoteLetter.D, 4), new(NoteLetter.E, 4)]),
        new([new(NoteLetter.G, 4), new(NoteLetter.E, 4), new(NoteLetter.C, 4)]),
        new([new(NoteLetter.C, 5), new(NoteLetter.B, 4), new(NoteLetter.A, 4)]),
        new([new(NoteLetter.E, 4), new(NoteLetter.G, 4), new(NoteLetter.F, 4)]),
        new([new(NoteLetter.C, 4), new(NoteLetter.E, 4), new(NoteLetter.G, 4), new(NoteLetter.E, 4)]),
        new([new(NoteLetter.G, 4), new(NoteLetter.A, 4), new(NoteLetter.G, 4), new(NoteLetter.E, 4)]),
        new([new(NoteLetter.C, 5), new(NoteLetter.B, 4), new(NoteLetter.A, 4), new(NoteLetter.G, 4)])
    ];

    private StaffPhrase staffPhrase = BeginnerStaffPhrases[0];
    private StaffPhrase? previousStaffPhrase;
    private PitchSequenceEvaluator? staffPhraseEvaluator;

    private bool IsStaffPhraseMode => mode == DrillMode.StaffPhrasePitch;
    private int StaffPhrasePosition => staffPhraseEvaluator?.Position ?? 0;
    private int StaffPhraseLength => staffPhrase.Pitches.Count;

    private void StartNewStaffPhrase()
    {
        var targetLength = CurrentLevelProgress.BestStreak switch
        {
            < 2 => 2,
            < 4 => 3,
            _ => 4
        };
        var candidates = BeginnerStaffPhrases
            .Where(phrase =>
                phrase.Pitches.Count == targetLength &&
                (previousStaffPhrase is null || !phrase.Pitches.SequenceEqual(previousStaffPhrase.Pitches)))
            .ToArray();

        staffPhrase = candidates[Random.Shared.Next(candidates.Length)];
        previousStaffPhrase = staffPhrase;
        staffPhraseEvaluator = staffPhrase.CreatePitchEvaluator();
        feedbackKey = "StaffPhraseReadyFeedback";
        feedbackArguments = [];
        feedbackClass = "feedback";
    }

    private async Task SubmitStaffPhraseNote(int midiNote)
    {
        if (staffPhraseEvaluator is null)
        {
            return;
        }

        var step = staffPhraseEvaluator.SubmitNote(midiNote);
        switch (step.Result)
        {
            case PitchSequenceStepResult.Incorrect:
                feedbackKey = "StaffPhraseTryAgainFeedback";
                feedbackArguments = [step.Position + 1];
                feedbackClass = "feedback is-missed";
                await Audio.PlayBuzzerAsync();
                break;

            case PitchSequenceStepResult.Correct:
                feedbackKey = "StaffPhraseProgressFeedback";
                feedbackArguments = [step.Position, StaffPhraseLength];
                feedbackClass = "feedback is-correct";
                break;

            case PitchSequenceStepResult.Completed:
                await CompleteStaffPhrase(step.Mistakes == 0);
                break;
        }
    }

    private async Task CompleteStaffPhrase(bool isPerfect)
    {
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

        feedbackKey = isPerfect ? "StaffPhraseCompleteFeedback" : "StaffPhraseCompleteWithMistakesFeedback";
        feedbackArguments = [];
        feedbackClass = isPerfect ? "feedback is-correct" : "feedback";
        await ProgressStore.SaveAsync(progress);
        await InvokeAsync(StateHasChanged);
        await Task.Delay(850);

        if (IsStaffPhraseMode)
        {
            StartNewStaffPhrase();
            await InvokeAsync(StateHasChanged);
        }
    }
}
