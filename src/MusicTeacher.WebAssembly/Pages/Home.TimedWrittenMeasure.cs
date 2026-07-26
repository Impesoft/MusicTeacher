using System.Diagnostics;
using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Practice;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private static readonly TimeSpan BeginnerPerformanceBeatInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan BeginnerPerformanceOnsetTolerance = TimeSpan.FromMilliseconds(450);
    private static readonly TimeSpan BeginnerPerformanceDurationTolerance = TimeSpan.FromMilliseconds(600);

    private static readonly IReadOnlyList<WrittenMeasure> BeginnerFourFourMeasures =
    [
        FourFour(new(NoteLetter.C, 4), new(NoteLetter.D, 4), new(NoteLetter.E, 4), new(NoteLetter.C, 4)),
        FourFour(new(NoteLetter.E, 4), new(NoteLetter.F, 4), new(NoteLetter.G, 4), new(NoteLetter.E, 4)),
        FourFour(new(NoteLetter.G, 4), new(NoteLetter.A, 4), new(NoteLetter.G, 4), new(NoteLetter.E, 4)),
        FourFour(new(NoteLetter.C, 5), new(NoteLetter.B, 4), new(NoteLetter.A, 4), new(NoteLetter.G, 4))
    ];

    private WrittenMeasure timedWrittenMeasure = BeginnerFourFourMeasures[0];
    private WrittenMeasure? previousTimedWrittenMeasure;
    private TimedWrittenMeasureEvaluator? timedMeasureEvaluator;
    private int timedMeasureVersion;
    private int timedMeasureCountIn;
    private int timedMeasureActiveBeat;
    private bool isTimedMeasureInputActive;
    private int? timedMeasureHeldMidiNote;
    private long timedMeasureStartedTimestamp;
    private long timedMeasureHoldStartedTimestamp;

    private bool IsTimedWrittenMeasureMode => mode == DrillMode.WrittenMeasureFourFour;
    private bool UsesWrittenMeasureInput
        => IsWrittenMeasureMode || IsTimedWrittenMeasureMode || IsGuidedSongTimedStage;
    private int TimedMeasurePosition => timedMeasureEvaluator?.Position ?? 0;
    private IReadOnlyList<Pitch> TimedMeasurePitches
        => timedWrittenMeasure.Notes.Select(note => note.Pitch).ToArray();

    private void PrepareTimedWrittenMeasure(bool chooseNewMeasure = true)
    {
        timedMeasureVersion++;
        timedMeasureCountIn = 0;
        timedMeasureActiveBeat = 0;
        isTimedMeasureInputActive = false;
        timedMeasureHeldMidiNote = null;
        timedMeasureEvaluator = null;
        if (chooseNewMeasure)
        {
            var candidates = BeginnerFourFourMeasures
                .Where(measure =>
                    previousTimedWrittenMeasure is null ||
                    !measure.Notes.SequenceEqual(previousTimedWrittenMeasure.Notes))
                .ToArray();
            timedWrittenMeasure = candidates[Random.Shared.Next(candidates.Length)];
            previousTimedWrittenMeasure = timedWrittenMeasure;
        }
        feedbackKey = "TimedMeasureReadyFeedback";
        feedbackArguments = [];
        feedbackClass = "feedback";
        _ = Audio.StopSustainedNoteAsync();
    }

    private async Task StartTimedMeasureCountIn()
    {
        var version = ++timedMeasureVersion;
        timedMeasureEvaluator = new(
            timedWrittenMeasure.Notes
                .Select(note => new WrittenNoteTarget(note.Pitch.MidiNote, note.DurationBeats))
                .ToArray(),
            BeginnerPerformanceBeatInterval,
            BeginnerPerformanceOnsetTolerance,
            BeginnerPerformanceDurationTolerance);
        feedbackClass = "feedback";

        for (var count = 1; count <= 4; count++)
        {
            if (!IsCurrentTimedMeasure(version)) return;
            timedMeasureCountIn = count;
            feedbackKey = "TimedMeasureCountInFeedback";
            feedbackArguments = [count];
            await Audio.PlayMidiNoteAsync(84);

            if (count == 4)
            {
                isTimedMeasureInputActive = true;
                timedMeasureStartedTimestamp = Stopwatch.GetTimestamp();
                _ = TrackTimedMeasure(version);
            }

            await InvokeAsync(StateHasChanged);
            await Task.Delay(BeginnerPerformanceBeatInterval);
        }

        if (!IsCurrentTimedMeasure(version)) return;
        timedMeasureCountIn = 0;
        feedbackKey = "TimedMeasurePlayFeedback";
        feedbackArguments = [];
        await InvokeAsync(StateHasChanged);
    }

    private async Task TrackTimedMeasure(int version)
    {
        var nextBeat = 1;
        while (IsCurrentTimedMeasure(version) && isTimedMeasureInputActive)
        {
            var elapsed = Stopwatch.GetElapsedTime(timedMeasureStartedTimestamp);
            if (nextBeat <= 5 && elapsed >= BeginnerPerformanceBeatInterval * nextBeat)
            {
                timedMeasureActiveBeat = nextBeat <= 4 ? nextBeat : 0;
                nextBeat++;
                await Audio.PlayMidiNoteAsync(84);
            }

            if (elapsed >= BeginnerPerformanceBeatInterval * 5 + TimeSpan.FromMilliseconds(400))
            {
                await CompleteTimedMeasure(false);
                return;
            }

            await InvokeAsync(StateHasChanged);
            await Task.Delay(35);
        }
    }

    private async Task StartPracticeMeasureNote(Pitch pitch)
        => await StartPracticeMeasureNote(pitch.MidiNote);

    private async Task StartPracticeMeasureNote(int midiNote)
    {
        if (IsWrittenMeasureMode)
        {
            await StartWrittenMeasureNote(midiNote);
            return;
        }

        if (IsGuidedSongTimedStage)
        {
            await StartGuidedSongTimedNote(midiNote);
            return;
        }

        if (!IsTimedWrittenMeasureMode || !isTimedMeasureInputActive || timedMeasureEvaluator is null)
        {
            return;
        }

        var step = timedMeasureEvaluator.StartNote(
            midiNote,
            Stopwatch.GetElapsedTime(timedMeasureStartedTimestamp));
        if (step.Result == TimedMeasureStartResult.IncorrectPitch)
        {
            feedbackKey = "TimedMeasureWrongPitchFeedback";
            feedbackArguments = [TimedMeasurePosition + 1];
            feedbackClass = "feedback is-missed";
            await Audio.PlayBuzzerAsync();
            return;
        }

        if (step.Result is TimedMeasureStartResult.AlreadyComplete or TimedMeasureStartResult.AlreadyHolding)
        {
            return;
        }

        timedMeasureHeldMidiNote = midiNote;
        timedMeasureHoldStartedTimestamp = Stopwatch.GetTimestamp();
        feedbackKey = step.Result switch
        {
            TimedMeasureStartResult.Early => "TimedMeasureEarlyFeedback",
            TimedMeasureStartResult.Late => "TimedMeasureLateFeedback",
            _ => "TimedMeasureOnBeatFeedback"
        };
        feedbackArguments = [];
        feedbackClass = step.Result == TimedMeasureStartResult.OnBeat
            ? "feedback is-correct"
            : "feedback is-missed";
        await Audio.StartSustainedMidiNoteAsync(midiNote);
    }

    private async Task EndPracticeMeasureNote(Pitch pitch)
        => await EndPracticeMeasureNote(pitch.MidiNote);

    private async Task EndPracticeMeasureNote(int midiNote)
    {
        if (IsWrittenMeasureMode)
        {
            await EndWrittenMeasureNote(midiNote);
            return;
        }

        if (IsGuidedSongTimedStage)
        {
            await EndGuidedSongTimedNote(midiNote);
            return;
        }

        if (!IsTimedWrittenMeasureMode ||
            timedMeasureEvaluator is null ||
            timedMeasureHeldMidiNote != midiNote)
        {
            return;
        }

        timedMeasureHeldMidiNote = null;
        await Audio.StopSustainedNoteAsync();
        var step = timedMeasureEvaluator.EndNote(
            midiNote,
            Stopwatch.GetElapsedTime(timedMeasureHoldStartedTimestamp));
        feedbackKey = step.Result switch
        {
            TimedMeasureEndResult.TooShort => "TimedMeasureTooShortFeedback",
            TimedMeasureEndResult.TooLong => "TimedMeasureTooLongFeedback",
            _ => "TimedMeasureProgressFeedback"
        };
        feedbackArguments = step.Result == TimedMeasureEndResult.OnTarget
            ? [TimedMeasurePosition, timedWrittenMeasure.Notes.Count]
            : [];
        feedbackClass = step.Result == TimedMeasureEndResult.OnTarget
            ? "feedback is-correct"
            : "feedback is-missed";

        if (step.IsComplete)
        {
            await CompleteTimedMeasure(step.Mistakes == 0);
        }
    }

    private async Task CompleteTimedMeasure(bool isPerfect)
    {
        timedMeasureVersion++;
        var wasGuidedSongUnlocked = IsModeUnlocked(DrillMode.GuidedSong);
        isTimedMeasureInputActive = false;
        timedMeasureCountIn = 0;
        timedMeasureHeldMidiNote = null;
        await Audio.StopSustainedNoteAsync();
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
        feedbackKey = isPerfect ? "TimedMeasureCompleteFeedback" : "TimedMeasureTryAgainFeedback";
        feedbackArguments = [];
        feedbackClass = isPerfect ? "feedback is-correct" : "feedback is-missed";
        if (practiceMode == PracticeMode.LearningPath &&
            isPerfect &&
            !wasGuidedSongUnlocked &&
            IsModeUnlocked(DrillMode.GuidedSong))
        {
            feedbackKey = "LevelUnlockedFeedback";
            feedbackArguments = [Localizer[GetModeLabelKey(DrillMode.GuidedSong)]];
            await AwardUnlock(DrillMode.GuidedSong);
        }
        await ProgressStore.SaveAsync(progress);
        await InvokeAsync(StateHasChanged);
        await Task.Delay(1_200);

        if (IsTimedWrittenMeasureMode)
        {
            PrepareTimedWrittenMeasure(isPerfect);
            await InvokeAsync(StateHasChanged);
        }
    }

    private bool IsCurrentTimedMeasure(int version)
        => version == timedMeasureVersion && IsTimedWrittenMeasureMode;

    private static WrittenMeasure FourFour(Pitch first, Pitch second, Pitch third, Pitch fourth)
        => new(
            TimeSignature.FourFour,
            [
                new WrittenMeasureNote(first, 1),
                new WrittenMeasureNote(second, 1),
                new WrittenMeasureNote(third, 1),
                new WrittenMeasureNote(fourth, 1)
            ]);
}
