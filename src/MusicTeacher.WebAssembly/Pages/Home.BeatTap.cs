using System.Diagnostics;
using MusicTeacher.Shared.Practice;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private static readonly TimeSpan BeatInterval = TimeSpan.FromMilliseconds(600);
    private const int RequiredBeatTaps = 8;

    private SteadyBeatEvaluator? beatEvaluator;
    private long beatRoundStartedTimestamp;
    private int beatCountIn;
    private int beatRoundVersion;
    private bool isBeatTapActive;

    private int BeatTapCount => beatEvaluator?.TapCount ?? 0;

    private void ResetBeatRound()
    {
        beatRoundVersion++;
        beatEvaluator = null;
        beatCountIn = 0;
        isBeatTapActive = false;
        feedbackKey = "BeatTapReadyFeedback";
        feedbackArguments = [];
        feedbackClass = "feedback";
    }

    private async Task StartBeatRound()
    {
        var version = ++beatRoundVersion;
        beatEvaluator = null;
        isBeatTapActive = false;
        feedbackClass = "feedback";

        for (var count = 4; count >= 1; count--)
        {
            if (version != beatRoundVersion || mode != DrillMode.BeatTap)
            {
                return;
            }

            beatCountIn = count;
            feedbackKey = "BeatTapCountInFeedback";
            feedbackArguments = [count];
            await Audio.PlayMidiNoteAsync(72);
            await InvokeAsync(StateHasChanged);
            await Task.Delay(BeatInterval);
        }

        if (version != beatRoundVersion || mode != DrillMode.BeatTap)
        {
            return;
        }

        beatCountIn = 0;
        beatEvaluator = new SteadyBeatEvaluator(BeatInterval, RequiredBeatTaps);
        beatRoundStartedTimestamp = Stopwatch.GetTimestamp();
        isBeatTapActive = true;
        feedbackKey = "BeatTapNowFeedback";
        feedbackArguments = [];
        await InvokeAsync(StateHasChanged);
    }

    private async Task RegisterBeatTap()
    {
        if (!isBeatTapActive || beatEvaluator is null)
        {
            return;
        }

        var elapsed = Stopwatch.GetElapsedTime(beatRoundStartedTimestamp);
        var step = beatEvaluator.SubmitTap(elapsed);
        await Audio.PlayMidiNoteAsync(60);

        feedbackKey = step.Guidance switch
        {
            BeatTapGuidance.Early => "BeatTapEarlyFeedback",
            BeatTapGuidance.Late => "BeatTapLateFeedback",
            _ => "BeatTapOnBeatFeedback"
        };
        feedbackArguments = [step.TapCount, step.RequiredTaps];
        feedbackClass = step.Guidance == BeatTapGuidance.OnBeat
            ? "feedback is-correct"
            : "feedback";

        if (!step.IsComplete)
        {
            return;
        }

        isBeatTapActive = false;
        await CompleteBeatRound(step.IsSuccessful);
    }

    private async Task CompleteBeatRound(bool isSuccessful)
    {
        var wasDurationHoldUnlocked = IsModeUnlocked(DrillMode.HoldDuration);
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

        feedbackKey = isSuccessful ? "BeatTapCompleteFeedback" : "BeatTapTryAgainFeedback";
        feedbackArguments = [];
        feedbackClass = isSuccessful ? "feedback is-correct" : "feedback is-missed";

        if (practiceMode == PracticeMode.LearningPath &&
            isSuccessful &&
            !wasDurationHoldUnlocked &&
            IsModeUnlocked(DrillMode.HoldDuration))
        {
            feedbackKey = "LevelUnlockedFeedback";
            feedbackArguments = [Localizer[GetModeLabelKey(DrillMode.HoldDuration)]];
            await AwardUnlock(DrillMode.HoldDuration);
        }

        await ProgressStore.SaveAsync(progress);
    }
}
