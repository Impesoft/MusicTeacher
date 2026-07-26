using System.Diagnostics;
using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Practice;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private static readonly IReadOnlyList<WrittenMeasure> BeginnerTwoFourMeasures =
    [
        TwoFour(new(NoteLetter.C, 4), new(NoteLetter.D, 4)),
        TwoFour(new(NoteLetter.E, 4), new(NoteLetter.C, 4)),
        TwoFour(new(NoteLetter.G, 4), new(NoteLetter.A, 4)),
        TwoFour(new(NoteLetter.C, 5), new(NoteLetter.B, 4)),
        TwoFour(new(NoteLetter.D, 4), new(NoteLetter.F, 4)),
        TwoFour(new(NoteLetter.A, 4), new(NoteLetter.G, 4))
    ];

    private WrittenMeasure writtenMeasure = BeginnerTwoFourMeasures[0];
    private WrittenMeasure? previousWrittenMeasure;
    private PitchDurationSequenceEvaluator? writtenMeasureEvaluator;
    private int? writtenMeasureHeldMidiNote;
    private long writtenMeasureHoldStartedTimestamp;
    private int writtenMeasureHoldVersion;

    private bool IsWrittenMeasureMode => mode == DrillMode.WrittenMeasureTwoFour;
    private int WrittenMeasurePosition => writtenMeasureEvaluator?.Position ?? 0;
    private IReadOnlyList<Pitch> WrittenMeasurePitches
        => writtenMeasure.Notes.Select(note => note.Pitch).ToArray();

    private void StartNewWrittenMeasure(bool chooseNewMeasure = true)
    {
        if (chooseNewMeasure)
        {
            var candidates = BeginnerTwoFourMeasures
                .Where(measure =>
                    previousWrittenMeasure is null ||
                    !measure.Notes.SequenceEqual(previousWrittenMeasure.Notes))
                .ToArray();
            writtenMeasure = candidates[Random.Shared.Next(candidates.Length)];
            previousWrittenMeasure = writtenMeasure;
        }
        writtenMeasureEvaluator = writtenMeasure.CreateEvaluator(BeatInterval);
        writtenMeasureHeldMidiNote = null;
        writtenMeasureHoldVersion++;
        feedbackKey = "WrittenMeasureReadyFeedback";
        feedbackArguments = [];
        feedbackClass = "feedback";
        _ = Audio.StopSustainedNoteAsync();
    }

    private async Task StartWrittenMeasureNote(Pitch pitch)
        => await StartWrittenMeasureNote(pitch.MidiNote);

    private async Task StartWrittenMeasureNote(int midiNote)
    {
        if (!IsWrittenMeasureMode || writtenMeasureEvaluator is null)
        {
            return;
        }

        var result = writtenMeasureEvaluator.StartNote(midiNote);
        if (result == PitchDurationStartResult.IncorrectPitch)
        {
            feedbackKey = "WrittenMeasureWrongPitchFeedback";
            feedbackArguments = [WrittenMeasurePosition + 1];
            feedbackClass = "feedback is-missed";
            await Audio.PlayBuzzerAsync();
            return;
        }

        if (result != PitchDurationStartResult.Started)
        {
            return;
        }

        writtenMeasureHeldMidiNote = midiNote;
        writtenMeasureHoldStartedTimestamp = Stopwatch.GetTimestamp();
        var version = ++writtenMeasureHoldVersion;
        feedbackKey = "WrittenMeasureHoldingFeedback";
        feedbackArguments = [WrittenMeasurePosition + 1];
        feedbackClass = "feedback";
        await Audio.StartSustainedMidiNoteAsync(midiNote);
        _ = CueWrittenMeasureRelease(version);
    }

    private async Task CueWrittenMeasureRelease(int version)
    {
        await Task.Delay(BeatInterval);
        if (version != writtenMeasureHoldVersion ||
            !IsWrittenMeasureMode ||
            writtenMeasureHeldMidiNote is null)
        {
            return;
        }

        feedbackKey = "WrittenMeasureReleaseFeedback";
        feedbackArguments = [];
        await Audio.PlayMidiNoteAsync(84);
        await InvokeAsync(StateHasChanged);
    }

    private async Task EndWrittenMeasureNote(Pitch pitch)
        => await EndWrittenMeasureNote(pitch.MidiNote);

    private async Task EndWrittenMeasureNote(int midiNote)
    {
        if (!IsWrittenMeasureMode ||
            writtenMeasureEvaluator is null ||
            writtenMeasureHeldMidiNote != midiNote)
        {
            return;
        }

        var heldDuration = Stopwatch.GetElapsedTime(writtenMeasureHoldStartedTimestamp);
        writtenMeasureHeldMidiNote = null;
        writtenMeasureHoldVersion++;
        await Audio.StopSustainedNoteAsync();
        var step = writtenMeasureEvaluator.EndNote(midiNote, heldDuration);
        switch (step.Result)
        {
            case PitchDurationStepResult.TooShort:
                feedbackKey = "WrittenMeasureTooShortFeedback";
                feedbackArguments = [WrittenMeasurePosition + 1];
                feedbackClass = "feedback is-missed";
                break;
            case PitchDurationStepResult.TooLong:
                feedbackKey = "WrittenMeasureTooLongFeedback";
                feedbackArguments = [WrittenMeasurePosition + 1];
                feedbackClass = "feedback is-missed";
                break;
            case PitchDurationStepResult.Correct:
                feedbackKey = "WrittenMeasureProgressFeedback";
                feedbackArguments = [WrittenMeasurePosition, writtenMeasure.Notes.Count];
                feedbackClass = "feedback is-correct";
                break;
            case PitchDurationStepResult.Completed:
                await CompleteWrittenMeasure(step.Mistakes == 0);
                break;
        }
    }

    private async Task CompleteWrittenMeasure(bool isPerfect)
    {
        var wasTimedMeasureUnlocked = IsModeUnlocked(DrillMode.WrittenMeasureFourFour);
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
        feedbackKey = isPerfect
            ? "WrittenMeasureCompleteFeedback"
            : "WrittenMeasureCompleteWithMistakesFeedback";
        feedbackArguments = [];
        feedbackClass = isPerfect ? "feedback is-correct" : "feedback";
        if (practiceMode == PracticeMode.LearningPath &&
            isPerfect &&
            !wasTimedMeasureUnlocked &&
            IsModeUnlocked(DrillMode.WrittenMeasureFourFour))
        {
            feedbackKey = "LevelUnlockedFeedback";
            feedbackArguments = [Localizer[GetModeLabelKey(DrillMode.WrittenMeasureFourFour)]];
            await AwardUnlock(DrillMode.WrittenMeasureFourFour);
        }
        await ProgressStore.SaveAsync(progress);
        await InvokeAsync(StateHasChanged);
        await Task.Delay(1_000);

        if (IsWrittenMeasureMode)
        {
            StartNewWrittenMeasure(isPerfect);
            await InvokeAsync(StateHasChanged);
        }
    }

    private static WrittenMeasure TwoFour(Pitch first, Pitch second)
        => new(
            TimeSignature.TwoFour,
            [
                new WrittenMeasureNote(first, 1),
                new WrittenMeasureNote(second, 1)
            ]);
}
