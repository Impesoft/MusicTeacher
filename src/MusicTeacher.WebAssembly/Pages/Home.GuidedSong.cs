using MusicTeacher.Shared.MusicTheory;
using MusicTeacher.Shared.Practice;
using MusicTeacher.Shared.Progress;
using MusicTeacher.Shared.Songs;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private static readonly SongDefinition GuidedSong = BeginnerSongLibrary.LevelComplete;
    private PitchSequenceEvaluator? guidedSongEvaluator;

    private bool IsGuidedSongMode => mode == DrillMode.GuidedSong;
    private SongLearningProgress GuidedSongProgress
        => progress.SongProgress?.GetValueOrDefault(GuidedSong.Id) ?? new();
    private int GuidedSongMeasureIndex
        => Math.Min(GuidedSongProgress.PitchMeasuresCompleted, GuidedSong.Measures.Count - 1);
    private SongMeasure CurrentGuidedSongMeasure => GuidedSong.Measures[GuidedSongMeasureIndex];
    private int GuidedSongPosition => guidedSongEvaluator?.Position ?? 0;
    private bool IsGuidedSongPitchComplete
        => GuidedSongProgress.PitchMeasuresCompleted >= GuidedSong.Measures.Count;
    private SongSection CurrentGuidedSongSection
        => GuidedSong.Sections.First(section => section.Measures.Contains(CurrentGuidedSongMeasure));
    private int GuidedSongMeasureInSection
        => CurrentGuidedSongSection.Measures.ToList().IndexOf(CurrentGuidedSongMeasure) + 1;
    private IReadOnlyList<Pitch> GuidedSongPitches
        => CurrentGuidedSongMeasure.WrittenMeasure.Notes.Select(note => note.Pitch).ToArray();

    private void PrepareGuidedSongMeasure()
    {
        guidedSongEvaluator = CurrentGuidedSongMeasure.WrittenMeasure.Notes.Count == 0
            ? null
            : new PitchSequenceEvaluator(
                CurrentGuidedSongMeasure.WrittenMeasure.Notes.Select(note => note.Pitch.MidiNote).ToArray());
        feedbackKey = IsGuidedSongPitchComplete
            ? "GuidedSongAllMeasuresCompleteFeedback"
            : "GuidedSongReadyFeedback";
        feedbackArguments = IsGuidedSongPitchComplete
            ? []
            : [GuidedSongMeasureInSection, CurrentGuidedSongSection.Measures.Count];
        feedbackClass = IsGuidedSongPitchComplete ? "feedback is-correct" : "feedback";
    }

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

        var completedSong = isPerfect &&
            songProgress[GuidedSong.Id].PitchMeasuresCompleted >= GuidedSong.Measures.Count;
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
            ? "GuidedSongRetryMeasureFeedback"
            : completedSong
                ? "GuidedSongAllMeasuresCompleteFeedback"
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
