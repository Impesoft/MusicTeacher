using System.Diagnostics;
using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Practice;
using MusicTeacher.Shared.Progress;
using MusicTeacher.Shared.Songs;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private static readonly SongDefinition GuidedSong = BeginnerSongLibrary.LevelComplete;
    private PitchSequenceEvaluator? guidedSongEvaluator;
    private TimedWrittenMeasureEvaluator? guidedSongTimedEvaluator;
    private int guidedSongTimedVersion;
    private int guidedSongCountIn;
    private int guidedSongActiveBeat;
    private bool isGuidedSongTimedInputActive;
    private int? guidedSongHeldMidiNote;
    private long guidedSongTimedStartedTimestamp;
    private long guidedSongHoldStartedTimestamp;

    private bool IsGuidedSongMode => mode == DrillMode.GuidedSong;
    private SongLearningProgress GuidedSongProgress
        => progress.SongProgress?.GetValueOrDefault(GuidedSong.Id) ?? new();
    private int GuidedSongMeasureIndex
        => !IsGuidedSongPitchComplete
            ? Math.Min(GuidedSongProgress.PitchMeasuresCompleted, GuidedSong.Measures.Count - 1)
            : Math.Min(GuidedSongProgress.TimedMeasuresCompleted, GuidedSong.Measures.Count - 1);
    private SongMeasure CurrentGuidedSongMeasure => GuidedSong.Measures[GuidedSongMeasureIndex];
    private int GuidedSongPosition => IsGuidedSongTimedStage
        ? guidedSongTimedEvaluator?.Position ?? 0
        : guidedSongEvaluator?.Position ?? 0;
    private bool IsGuidedSongPitchComplete
        => GuidedSongProgress.PitchMeasuresCompleted >= GuidedSong.Measures.Count;
    private bool IsGuidedSongTimedStage => IsGuidedSongPitchComplete && !IsGuidedSongComplete;
    private bool IsGuidedSongComplete
        => GuidedSongProgress.TimedMeasuresCompleted >= GuidedSong.Measures.Count;
    private SongSection CurrentGuidedSongSection
        => GuidedSong.Sections.First(section => section.Measures.Contains(CurrentGuidedSongMeasure));
    private int GuidedSongMeasureInSection
        => CurrentGuidedSongSection.Measures.ToList().IndexOf(CurrentGuidedSongMeasure) + 1;
    private IReadOnlyList<Pitch> GuidedSongPitches
        => CurrentGuidedSongMeasure.WrittenMeasure.Notes.Select(note => note.Pitch).ToArray();

    private void PrepareGuidedSongMeasure()
    {
        guidedSongTimedVersion++;
        guidedSongCountIn = 0;
        guidedSongActiveBeat = 0;
        isGuidedSongTimedInputActive = false;
        guidedSongHeldMidiNote = null;
        guidedSongTimedEvaluator = null;
        guidedSongEvaluator = IsGuidedSongTimedStage || IsGuidedSongComplete
            ? null
            : new PitchSequenceEvaluator(
                CurrentGuidedSongMeasure.WrittenMeasure.Notes.Select(note => note.Pitch.MidiNote).ToArray());
        feedbackKey = IsGuidedSongComplete
            ? "GuidedSongAllMeasuresCompleteFeedback"
            : IsGuidedSongTimedStage
                ? "GuidedSongTimedReadyFeedback"
            : "GuidedSongReadyFeedback";
        feedbackArguments = IsGuidedSongComplete
            ? []
            : [GuidedSongMeasureInSection, CurrentGuidedSongSection.Measures.Count];
        feedbackClass = IsGuidedSongComplete ? "feedback is-correct" : "feedback";
        _ = Audio.StopSustainedNoteAsync();
    }

    private async Task StartGuidedSongCountIn()
    {
        if (!IsGuidedSongTimedStage) return;
        var version = ++guidedSongTimedVersion;
        guidedSongTimedEvaluator = new(
            CurrentGuidedSongMeasure.WrittenMeasure.Notes
                .Select(note => new WrittenNoteTarget(note.Pitch.MidiNote, note.DurationBeats))
                .ToArray(),
            BeginnerPerformanceBeatInterval,
            BeginnerPerformanceOnsetTolerance,
            BeginnerPerformanceDurationTolerance);

        for (var count = 1; count <= 4; count++)
        {
            if (!IsCurrentGuidedSongTimedVersion(version)) return;
            guidedSongCountIn = count;
            feedbackKey = "GuidedSongTimedCountInFeedback";
            feedbackArguments = [count];
            await Audio.PlayMidiNoteAsync(84);
            if (count == 4)
            {
                guidedSongTimedStartedTimestamp = Stopwatch.GetTimestamp();
            }
            await InvokeAsync(StateHasChanged);
            await Task.Delay(BeginnerPerformanceBeatInterval);
        }

        if (!IsCurrentGuidedSongTimedVersion(version)) return;
        guidedSongCountIn = 0;
        isGuidedSongTimedInputActive = true;
        feedbackKey = "GuidedSongTimedPlayFeedback";
        feedbackArguments = [];
        _ = TrackGuidedSongTimedMeasure(version);
        await InvokeAsync(StateHasChanged);
    }

    private async Task TrackGuidedSongTimedMeasure(int version)
    {
        var nextBeat = 1;
        while (IsCurrentGuidedSongTimedVersion(version) && isGuidedSongTimedInputActive)
        {
            var elapsed = Stopwatch.GetElapsedTime(guidedSongTimedStartedTimestamp);
            if (nextBeat <= 5 && elapsed >= BeginnerPerformanceBeatInterval * nextBeat)
            {
                guidedSongActiveBeat = nextBeat <= 4 ? nextBeat : 0;
                nextBeat++;
                await Audio.PlayMidiNoteAsync(84);
            }

            if (elapsed >= BeginnerPerformanceBeatInterval * 5 + TimeSpan.FromMilliseconds(400))
            {
                await CompleteGuidedSongTimedMeasure(false);
                return;
            }

            await InvokeAsync(StateHasChanged);
            await Task.Delay(35);
        }
    }

    private async Task StartGuidedSongTimedNote(int midiNote)
    {
        if (!IsGuidedSongTimedStage ||
            !isGuidedSongTimedInputActive ||
            guidedSongTimedEvaluator is null)
        {
            return;
        }

        var step = guidedSongTimedEvaluator.StartNote(
            midiNote,
            Stopwatch.GetElapsedTime(guidedSongTimedStartedTimestamp));
        if (step.Result == TimedMeasureStartResult.IncorrectPitch)
        {
            feedbackKey = "GuidedSongWrongPitchFeedback";
            feedbackArguments = [GuidedSongPosition + 1];
            feedbackClass = "feedback is-missed";
            await Audio.PlayBuzzerAsync();
            return;
        }

        if (step.Result is TimedMeasureStartResult.AlreadyComplete or TimedMeasureStartResult.AlreadyHolding)
        {
            return;
        }

        guidedSongHeldMidiNote = midiNote;
        guidedSongHoldStartedTimestamp = Stopwatch.GetTimestamp();
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

    private async Task EndGuidedSongTimedNote(int midiNote)
    {
        if (!IsGuidedSongTimedStage ||
            guidedSongTimedEvaluator is null ||
            guidedSongHeldMidiNote != midiNote)
        {
            return;
        }

        guidedSongHeldMidiNote = null;
        await Audio.StopSustainedNoteAsync();
        var step = guidedSongTimedEvaluator.EndNote(
            midiNote,
            Stopwatch.GetElapsedTime(guidedSongHoldStartedTimestamp));
        feedbackKey = step.Result switch
        {
            TimedMeasureEndResult.TooShort => "TimedMeasureTooShortFeedback",
            TimedMeasureEndResult.TooLong => "TimedMeasureTooLongFeedback",
            _ => "GuidedSongTimedProgressFeedback"
        };
        feedbackArguments = step.Result == TimedMeasureEndResult.OnTarget
            ? [GuidedSongPosition, GuidedSongPitches.Count]
            : [];
        feedbackClass = step.Result == TimedMeasureEndResult.OnTarget
            ? "feedback is-correct"
            : "feedback is-missed";
        if (step.IsComplete)
        {
            await CompleteGuidedSongTimedMeasure(step.Mistakes == 0);
        }
    }

    private async Task CompleteGuidedSongTimedMeasure(bool isPerfect)
    {
        guidedSongTimedVersion++;
        isGuidedSongTimedInputActive = false;
        guidedSongHeldMidiNote = null;
        await Audio.StopSustainedNoteAsync();
        var songProgress = progress.SongProgress is null
            ? []
            : new Dictionary<string, SongLearningProgress>(progress.SongProgress);
        if (isPerfect)
        {
            songProgress[GuidedSong.Id] = GuidedSongProgress with
            {
                TimedMeasuresCompleted = GuidedSongProgress.TimedMeasuresCompleted + 1
            };
        }

        var completedSong = isPerfect &&
            songProgress[GuidedSong.Id].TimedMeasuresCompleted >= GuidedSong.Measures.Count;
        var drillProgress = completedSong && practiceMode == PracticeMode.LearningPath
            ? UpdateCurrentDrillProgress(true)
            : progress.DrillProgress;
        progress = progress with
        {
            Attempts = progress.Attempts + 1,
            CorrectAnswers = progress.CorrectAnswers + (isPerfect ? 1 : 0),
            Streak = isPerfect ? progress.Streak + 1 : 0,
            DrillProgress = drillProgress,
            SongProgress = songProgress
        };
        feedbackKey = !isPerfect
            ? "GuidedSongTimedRetryFeedback"
            : completedSong
                ? "GuidedSongCompleteFeedback"
                : "GuidedSongTimedMeasureCompleteFeedback";
        feedbackArguments = [];
        feedbackClass = isPerfect ? "feedback is-correct" : "feedback is-missed";
        await ProgressStore.SaveAsync(progress);
        await InvokeAsync(StateHasChanged);
        await Task.Delay(1_000);
        if (IsGuidedSongMode)
        {
            PrepareGuidedSongMeasure();
            await InvokeAsync(StateHasChanged);
        }
    }

    private bool IsCurrentGuidedSongTimedVersion(int version)
        => version == guidedSongTimedVersion && IsGuidedSongMode;

    private async Task SubmitGuidedSongNote(int midiNote)
    {
        if (!IsGuidedSongMode || IsGuidedSongPitchComplete || guidedSongEvaluator is null)
        {
            return;
        }

        var step = guidedSongEvaluator.SubmitNote(midiNote);
        if (step.Result == PitchSequenceStepResult.Incorrect)
        {
            feedbackKey = "GuidedSongWrongPitchFeedback";
            feedbackArguments = [step.Position + 1];
            feedbackClass = "feedback is-missed";
            await Audio.PlayBuzzerAsync();
            return;
        }

        if (step.Result == PitchSequenceStepResult.Correct)
        {
            feedbackKey = "GuidedSongProgressFeedback";
            feedbackArguments = [step.Position, GuidedSongPitches.Count];
            feedbackClass = "feedback is-correct";
            return;
        }

        if (step.Result == PitchSequenceStepResult.Completed)
        {
            await CompleteGuidedSongMeasure(step.Mistakes == 0);
        }
    }

    private async Task CompleteGuidedSongMeasure(bool isPerfect)
    {
        var songProgress = progress.SongProgress is null
            ? []
            : new Dictionary<string, SongLearningProgress>(progress.SongProgress);
        if (isPerfect)
        {
            songProgress[GuidedSong.Id] = GuidedSongProgress with
            {
                PitchMeasuresCompleted = GuidedSongProgress.PitchMeasuresCompleted + 1
            };
        }

        progress = progress with
        {
            Attempts = progress.Attempts + 1,
            CorrectAnswers = progress.CorrectAnswers + (isPerfect ? 1 : 0),
            Streak = isPerfect ? progress.Streak + 1 : 0,
            SongProgress = songProgress
        };
        feedbackKey = !isPerfect
            ? "GuidedSongRetryMeasureFeedback"
            : "GuidedSongMeasureCompleteFeedback";
        feedbackArguments = [];
        feedbackClass = isPerfect ? "feedback is-correct" : "feedback is-missed";
        await ProgressStore.SaveAsync(progress);
        await InvokeAsync(StateHasChanged);
        await Task.Delay(1_000);

        if (IsGuidedSongMode)
        {
            PrepareGuidedSongMeasure();
            await InvokeAsync(StateHasChanged);
        }
    }
}
