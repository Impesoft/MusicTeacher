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
    private bool IsGuidedSongPitchComplete
        => GuidedSongProgress.PitchMeasuresCompleted >= GuidedSong.Measures.Count;
    private bool IsGuidedSongTimedMeasureStage
        => IsGuidedSongPitchComplete &&
            GuidedSongProgress.TimedMeasuresCompleted < GuidedSong.Measures.Count;
    private bool IsGuidedSongSectionStage
        => GuidedSongProgress.TimedMeasuresCompleted >= GuidedSong.Measures.Count &&
            GuidedSongProgress.SectionsCompleted < GuidedSong.Sections.Count;
    private bool IsGuidedSongFullStage
        => GuidedSongProgress.SectionsCompleted >= GuidedSong.Sections.Count &&
            !GuidedSongProgress.CompleteSongCompleted;
    private bool IsGuidedSongTimedStage
        => IsGuidedSongTimedMeasureStage || IsGuidedSongSectionStage || IsGuidedSongFullStage;
    private bool IsGuidedSongComplete => GuidedSongProgress.CompleteSongCompleted;

    private IReadOnlyList<SongMeasure> GuidedSongPerformanceMeasures
        => IsGuidedSongTimedMeasureStage
            ? [GuidedSong.Measures[Math.Min(GuidedSongProgress.TimedMeasuresCompleted, GuidedSong.Measures.Count - 1)]]
            : IsGuidedSongSectionStage
                ? GuidedSong.Sections[Math.Min(GuidedSongProgress.SectionsCompleted, GuidedSong.Sections.Count - 1)].Measures
                : IsGuidedSongFullStage
                    ? GuidedSong.Measures
                    : [];
    private int GuidedSongPerformanceNoteCount
        => GuidedSongPerformanceMeasures.Sum(measure => measure.WrittenMeasure.Notes.Count);
    private int GuidedSongPerformanceMeasureOffset
        => GuidedSongPerformanceMeasures.Count == 0
            ? 0
            : Math.Min(
                (guidedSongTimedEvaluator?.Position ?? 0) / 4,
                GuidedSongPerformanceMeasures.Count - 1);
    private int GuidedSongMeasureIndex
        => !IsGuidedSongPitchComplete
            ? Math.Min(GuidedSongProgress.PitchMeasuresCompleted, GuidedSong.Measures.Count - 1)
            : IsGuidedSongTimedStage
                ? GuidedSong.Measures.ToList().IndexOf(
                    GuidedSongPerformanceMeasures[GuidedSongPerformanceMeasureOffset])
                : GuidedSong.Measures.Count - 1;
    private SongMeasure CurrentGuidedSongMeasure => GuidedSong.Measures[GuidedSongMeasureIndex];
    private SongSection CurrentGuidedSongSection
        => GuidedSong.Sections.First(section => section.Measures.Contains(CurrentGuidedSongMeasure));
    private int GuidedSongMeasureInSection
        => CurrentGuidedSongSection.Measures.ToList().IndexOf(CurrentGuidedSongMeasure) + 1;
    private int GuidedSongPosition => IsGuidedSongTimedStage
        ? guidedSongTimedEvaluator?.IsComplete == true
            ? CurrentGuidedSongMeasure.WrittenMeasure.Notes.Count
            : (guidedSongTimedEvaluator?.Position ?? 0) % 4
        : guidedSongEvaluator?.Position ?? 0;
    private int GuidedSongOverallPerformancePosition => guidedSongTimedEvaluator?.Position ?? 0;
    private int GuidedSongCheckpointCount => IsGuidedSongSectionStage ? GuidedSong.Sections.Count : GuidedSong.Measures.Count;
    private int GuidedSongCompletedCheckpoints => IsGuidedSongComplete
        ? GuidedSong.Measures.Count
        : IsGuidedSongFullStage
            ? GuidedSongPerformanceMeasureOffset
            : IsGuidedSongSectionStage
                ? GuidedSongProgress.SectionsCompleted
                : IsGuidedSongTimedMeasureStage
                    ? GuidedSongProgress.TimedMeasuresCompleted
                    : GuidedSongProgress.PitchMeasuresCompleted;
    private int GuidedSongCurrentCheckpoint => IsGuidedSongSectionStage
        ? GuidedSongProgress.SectionsCompleted
        : GuidedSongMeasureIndex;
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
        guidedSongEvaluator = IsGuidedSongPitchComplete
            ? null
            : new PitchSequenceEvaluator(
                CurrentGuidedSongMeasure.WrittenMeasure.Notes.Select(note => note.Pitch.MidiNote).ToArray());
        feedbackKey = IsGuidedSongComplete
            ? "GuidedSongCompleteFeedback"
            : IsGuidedSongFullStage
                ? "GuidedSongFullReadyFeedback"
                : IsGuidedSongSectionStage
                    ? "GuidedSongSectionReadyFeedback"
                    : IsGuidedSongTimedMeasureStage
                        ? "GuidedSongTimedReadyFeedback"
                        : "GuidedSongReadyFeedback";
        feedbackArguments = IsGuidedSongComplete
            ? []
            : IsGuidedSongSectionStage
                ? [Localizer[CurrentGuidedSongSection.TitleKey]]
                : IsGuidedSongFullStage
                    ? []
                    : [GuidedSongMeasureInSection, CurrentGuidedSongSection.Measures.Count];
        feedbackClass = IsGuidedSongComplete ? "feedback is-correct" : "feedback";
        _ = Audio.StopSustainedNoteAsync();
    }

    private async Task SubmitGuidedSongNote(int midiNote)
    {
        if (!IsGuidedSongMode || IsGuidedSongPitchComplete || guidedSongEvaluator is null) return;
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
            await CompleteGuidedSongPitchMeasure(step.Mistakes == 0);
        }
    }

    private async Task CompleteGuidedSongPitchMeasure(bool isPerfect)
    {
        var songProgress = CopySongProgress();
        if (isPerfect)
        {
            songProgress[GuidedSong.Id] = GuidedSongProgress with
            {
                PitchMeasuresCompleted = GuidedSongProgress.PitchMeasuresCompleted + 1
            };
        }
        await SaveGuidedSongAttempt(
            songProgress,
            isPerfect,
            isPerfect ? "GuidedSongMeasureCompleteFeedback" : "GuidedSongRetryMeasureFeedback");
    }

    private async Task StartGuidedSongCountIn()
    {
        if (!IsGuidedSongTimedStage) return;
        var version = ++guidedSongTimedVersion;
        guidedSongTimedEvaluator = new(
            GuidedSongPerformanceMeasures
                .SelectMany(measure => measure.WrittenMeasure.Notes)
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
            if (count == 4) guidedSongTimedStartedTimestamp = Stopwatch.GetTimestamp();
            await InvokeAsync(StateHasChanged);
            await Task.Delay(BeginnerPerformanceBeatInterval);
        }

        if (!IsCurrentGuidedSongTimedVersion(version)) return;
        guidedSongCountIn = 0;
        isGuidedSongTimedInputActive = true;
        feedbackKey = IsGuidedSongFullStage
            ? "GuidedSongFullPlayFeedback"
            : IsGuidedSongSectionStage
                ? "GuidedSongSectionPlayFeedback"
                : "GuidedSongTimedPlayFeedback";
        feedbackArguments = [];
        _ = TrackGuidedSongPerformance(version);
        await InvokeAsync(StateHasChanged);
    }

    private async Task TrackGuidedSongPerformance(int version)
    {
        var nextBeat = 1;
        var finalReleaseBeat = GuidedSongPerformanceNoteCount + 1;
        while (IsCurrentGuidedSongTimedVersion(version) && isGuidedSongTimedInputActive)
        {
            var elapsed = Stopwatch.GetElapsedTime(guidedSongTimedStartedTimestamp);
            if (nextBeat <= finalReleaseBeat &&
                elapsed >= BeginnerPerformanceBeatInterval * nextBeat)
            {
                guidedSongActiveBeat = nextBeat <= GuidedSongPerformanceNoteCount
                    ? (nextBeat - 1) % 4 + 1
                    : 0;
                nextBeat++;
                await Audio.PlayMidiNoteAsync(84);
            }

            if (elapsed >= BeginnerPerformanceBeatInterval * finalReleaseBeat +
                TimeSpan.FromMilliseconds(400))
            {
                await CompleteGuidedSongPerformance(false);
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
            guidedSongTimedEvaluator is null) return;
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
            return;
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
            guidedSongHeldMidiNote != midiNote) return;
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
            ? [GuidedSongOverallPerformancePosition, GuidedSongPerformanceNoteCount]
            : [];
        feedbackClass = step.Result == TimedMeasureEndResult.OnTarget
            ? "feedback is-correct"
            : "feedback is-missed";
        if (step.IsComplete) await CompleteGuidedSongPerformance(step.Mistakes == 0);
    }

    private async Task CompleteGuidedSongPerformance(bool isPerfect)
    {
        var wasMeasureStage = IsGuidedSongTimedMeasureStage;
        var wasSectionStage = IsGuidedSongSectionStage;
        var wasFullStage = IsGuidedSongFullStage;
        guidedSongTimedVersion++;
        isGuidedSongTimedInputActive = false;
        guidedSongHeldMidiNote = null;
        await Audio.StopSustainedNoteAsync();
        var songProgress = CopySongProgress();
        if (isPerfect)
        {
            songProgress[GuidedSong.Id] = GuidedSongProgress with
            {
                TimedMeasuresCompleted = wasMeasureStage
                    ? GuidedSongProgress.TimedMeasuresCompleted + 1
                    : GuidedSongProgress.TimedMeasuresCompleted,
                SectionsCompleted = wasSectionStage
                    ? GuidedSongProgress.SectionsCompleted + 1
                    : GuidedSongProgress.SectionsCompleted,
                CompleteSongCompleted = wasFullStage || GuidedSongProgress.CompleteSongCompleted
            };
        }

        var completedSong = isPerfect && wasFullStage;
        var drillProgress = completedSong && practiceMode == PracticeMode.LearningPath
            ? UpdateCurrentDrillProgress(true)
            : progress.DrillProgress;
        var successKey = wasMeasureStage
            ? "GuidedSongTimedMeasureCompleteFeedback"
            : wasSectionStage
                ? "GuidedSongSectionCompleteFeedback"
                : "GuidedSongCompleteFeedback";
        await SaveGuidedSongAttempt(
            songProgress,
            isPerfect,
            isPerfect ? successKey : "GuidedSongTimedRetryFeedback",
            drillProgress);
    }

    private async Task SaveGuidedSongAttempt(
        Dictionary<string, SongLearningProgress> songProgress,
        bool isPerfect,
        string resultKey,
        Dictionary<string, DrillLevelProgress>? drillProgress = null)
    {
        progress = progress with
        {
            Attempts = progress.Attempts + 1,
            CorrectAnswers = progress.CorrectAnswers + (isPerfect ? 1 : 0),
            Streak = isPerfect ? progress.Streak + 1 : 0,
            DrillProgress = drillProgress ?? progress.DrillProgress,
            SongProgress = songProgress
        };
        feedbackKey = resultKey;
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

    private Dictionary<string, SongLearningProgress> CopySongProgress()
        => progress.SongProgress is null
            ? []
            : new Dictionary<string, SongLearningProgress>(progress.SongProgress);

    private bool IsCurrentGuidedSongTimedVersion(int version)
        => version == guidedSongTimedVersion && IsGuidedSongMode;
}
