using System.Diagnostics;
using MusicTeacher.Shared.Practice;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private static readonly IReadOnlyList<IReadOnlyList<int>> BeginnerRhythmPatterns =
    [
        [0, 2],
        [0, 1, 3],
        [0, 2, 3],
        [0, 1, 2]
    ];

    private IReadOnlyList<int> rhythmPattern = BeginnerRhythmPatterns[0];
    private IReadOnlyList<int>? previousRhythmPattern;
    private RhythmPatternEvaluator? rhythmEvaluator;
    private long rhythmStartedTimestamp;
    private int rhythmEchoVersion;
    private int rhythmActiveBeat;
    private bool isDemonstratingRhythm;
    private bool isRhythmInputActive;

    private void PrepareRhythmEchoRound(bool chooseNewPattern = true)
    {
        rhythmEchoVersion++;
        isDemonstratingRhythm = false;
        isRhythmInputActive = false;
        rhythmActiveBeat = 0;
        rhythmEvaluator = null;

        if (chooseNewPattern)
        {
            var choices = BeginnerRhythmPatterns
                .Where(pattern => previousRhythmPattern is null || !pattern.SequenceEqual(previousRhythmPattern))
                .ToArray();
            rhythmPattern = choices[Random.Shared.Next(choices.Length)];
            previousRhythmPattern = rhythmPattern;
        }
        feedbackKey = "RhythmEchoReadyFeedback";
        feedbackArguments = [];
        feedbackClass = "feedback";
    }

    private async Task DemonstrateRhythmPattern()
    {
        var version = ++rhythmEchoVersion;
        isDemonstratingRhythm = true;
        isRhythmInputActive = false;
        feedbackClass = "feedback";

        for (var count = 1; count <= 4; count++)
        {
            if (!IsCurrentRhythmVersion(version)) return;
            feedbackKey = "RhythmEchoCountInFeedback";
            feedbackArguments = [count];
            await Audio.PlayMidiNoteAsync(84);
            await InvokeAsync(StateHasChanged);
            await Task.Delay(BeatInterval);
        }

        for (var beat = 0; beat < 4; beat++)
        {
            if (!IsCurrentRhythmVersion(version)) return;
            rhythmActiveBeat = beat + 1;
            feedbackKey = "RhythmEchoListenFeedback";
            feedbackArguments = [beat + 1];
            await Audio.PlayMidiNoteAsync(84);
            if (rhythmPattern.Contains(beat))
            {
                await Audio.PlayDurationExampleAsync(1);
            }
            await InvokeAsync(StateHasChanged);
            await Task.Delay(BeatInterval);
        }

        for (var count = 1; count <= 4; count++)
        {
            if (!IsCurrentRhythmVersion(version)) return;
            rhythmActiveBeat = 0;
            feedbackKey = "RhythmEchoYourCountInFeedback";
            feedbackArguments = [count];
            await Audio.PlayMidiNoteAsync(84);

            if (count == 4)
            {
                isDemonstratingRhythm = false;
                isRhythmInputActive = true;
                rhythmEvaluator = new RhythmPatternEvaluator(
                    rhythmPattern.Select(beat => beat + 1).ToArray(),
                    BeatInterval);
                rhythmStartedTimestamp = Stopwatch.GetTimestamp();
                _ = TrackRhythmAttempt(version);
            }

            await InvokeAsync(StateHasChanged);
            await Task.Delay(BeatInterval);
        }

        if (!IsCurrentRhythmVersion(version) || !isRhythmInputActive) return;
        feedbackKey = "RhythmEchoYourTurnFeedback";
        feedbackArguments = [];
        await InvokeAsync(StateHasChanged);
    }

    private async Task TrackRhythmAttempt(int version)
    {
        var nextBeat = 1;
        while (IsCurrentRhythmVersion(version) && isRhythmInputActive)
        {
            var elapsed = Stopwatch.GetElapsedTime(rhythmStartedTimestamp);
            if (nextBeat <= 4 && elapsed >= BeatInterval * nextBeat)
            {
                rhythmActiveBeat = nextBeat;
                await Audio.PlayMidiNoteAsync(84);
                nextBeat++;
            }

            if (elapsed >= BeatInterval * 5 + TimeSpan.FromMilliseconds(250))
            {
                isRhythmInputActive = false;
                await CompleteRhythmEchoRound(false);
                return;
            }

            await InvokeAsync(StateHasChanged);
            await Task.Delay(40);
        }
    }

    private async Task RegisterRhythmTap()
    {
        if (!isRhythmInputActive || rhythmEvaluator is null)
        {
            return;
        }

        var step = rhythmEvaluator.SubmitTap(Stopwatch.GetElapsedTime(rhythmStartedTimestamp));
        await Audio.PlayMidiNoteAsync(60);
        feedbackKey = step.Guidance switch
        {
            RhythmTapGuidance.Early => "RhythmEchoEarlyFeedback",
            RhythmTapGuidance.Late => "RhythmEchoLateFeedback",
            _ => "RhythmEchoOnBeatFeedback"
        };
        feedbackArguments = [];
        feedbackClass = step.Guidance == RhythmTapGuidance.OnBeat ? "feedback is-correct" : "feedback";

        if (step.IsComplete)
        {
            isRhythmInputActive = false;
            await CompleteRhythmEchoRound(step.IsSuccessful);
        }
    }

    private async Task HandleRhythmAction()
    {
        if (isRhythmInputActive)
        {
            await RegisterRhythmTap();
        }
        else if (!isDemonstratingRhythm)
        {
            await DemonstrateRhythmPattern();
        }
    }

    private async Task CompleteRhythmEchoRound(bool isSuccessful)
    {
        rhythmEchoVersion++;
        var wasTimedMeasureUnlocked = IsModeUnlocked(DrillMode.WrittenMeasureFourFour);
        var updatedDrillProgress = practiceMode == PracticeMode.LearningPath
            ? UpdateCurrentDrillProgress(isSuccessful)
            : progress.DrillProgress;
        progress = progress with
        {
            Attempts = progress.Attempts + 1,
            CorrectAnswers = progress.CorrectAnswers + (isSuccessful ? 1 : 0),
            Streak = isSuccessful ? progress.Streak + 1 : 0,
            DrillProgress = updatedDrillProgress
        };
        feedbackKey = isSuccessful ? "RhythmEchoCompleteFeedback" : "RhythmEchoTryAgainFeedback";
        feedbackArguments = [];
        feedbackClass = isSuccessful ? "feedback is-correct" : "feedback is-missed";
        if (practiceMode == PracticeMode.LearningPath &&
            isSuccessful &&
            !wasTimedMeasureUnlocked &&
            IsModeUnlocked(DrillMode.WrittenMeasureFourFour))
        {
            feedbackKey = "LevelUnlockedFeedback";
            feedbackArguments = [Localizer[GetModeLabelKey(DrillMode.WrittenMeasureFourFour)]];
            await AwardUnlock(DrillMode.WrittenMeasureFourFour);
        }
        await ProgressStore.SaveAsync(progress);
        await InvokeAsync(StateHasChanged);
        await Task.Delay(1_200);

        if (mode == DrillMode.RhythmEcho)
        {
            PrepareRhythmEchoRound(isSuccessful);
            await InvokeAsync(StateHasChanged);
        }
    }

    private bool IsCurrentRhythmVersion(int version)
        => version == rhythmEchoVersion && mode == DrillMode.RhythmEcho;
}
