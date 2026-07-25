using System.Diagnostics;
using MusicTeacher.Shared.Practice;
using Microsoft.AspNetCore.Components.Web;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private static readonly int[] DurationBeatChoices = [1, 2, 4];

    private int requestedDurationBeats = 1;
    private int? previousDurationBeats;
    private int? heldDurationMidiNote;
    private long durationHoldStartedTimestamp;
    private int durationHoldVersion;
    private bool isHoldingDuration;
    private bool isDurationReadyToHold;
    private int durationCountIn;
    private TimeSpan currentHeldDuration;

    private string DurationProgressStyle
    {
        get
        {
            var rulerMilliseconds = BeatInterval.TotalMilliseconds * 4;
            var percent = rulerMilliseconds <= 0
                ? 0
                : Math.Clamp(currentHeldDuration.TotalMilliseconds / rulerMilliseconds * 100, 0, 100);
            return FormattableString.Invariant($"width: {percent:0.#}%;");
        }
    }

    private string DurationProgressBeats
        => FormattableString.Invariant(
            $"{Math.Min(currentHeldDuration.TotalMilliseconds / BeatInterval.TotalMilliseconds, 4):0.##}");

    private void PrepareDurationRound()
    {
        durationHoldVersion++;
        isHoldingDuration = false;
        heldDurationMidiNote = null;
        currentHeldDuration = TimeSpan.Zero;
        isDurationReadyToHold = false;
        durationCountIn = 0;
        _ = Audio.StopSustainedNoteAsync();

        var choices = DurationBeatChoices
            .Where(beats => beats != previousDurationBeats)
            .ToArray();
        requestedDurationBeats = choices[Random.Shared.Next(choices.Length)];
        previousDurationBeats = requestedDurationBeats;
        feedbackKey = "DurationHoldReadyFeedback";
        feedbackArguments = [];
        feedbackClass = "feedback";
    }

    private async Task StartDurationCountIn()
    {
        if (mode != DrillMode.HoldDuration || isHoldingDuration || durationCountIn > 0)
        {
            return;
        }

        var version = ++durationHoldVersion;
        isDurationReadyToHold = false;
        feedbackClass = "feedback";

        for (var count = 4; count >= 1; count--)
        {
            if (version != durationHoldVersion || mode != DrillMode.HoldDuration)
            {
                return;
            }

            durationCountIn = count;
            feedbackKey = "DurationHoldCountInFeedback";
            feedbackArguments = [count];
            await Audio.PlayMidiNoteAsync(72);
            await InvokeAsync(StateHasChanged);
            await Task.Delay(BeatInterval);
        }

        if (version != durationHoldVersion || mode != DrillMode.HoldDuration)
        {
            return;
        }

        durationCountIn = 0;
        isDurationReadyToHold = true;
        feedbackKey = "DurationHoldNowFeedback";
        feedbackArguments = [requestedDurationBeats];
        await InvokeAsync(StateHasChanged);
    }

    private async Task CancelDurationHold()
    {
        durationHoldVersion++;
        isHoldingDuration = false;
        heldDurationMidiNote = null;
        currentHeldDuration = TimeSpan.Zero;
        isDurationReadyToHold = false;
        durationCountIn = 0;
        await Audio.StopSustainedNoteAsync();
    }

    private async Task StartDurationHold(int? midiNote = null)
    {
        if (mode != DrillMode.HoldDuration || isHoldingDuration || !isDurationReadyToHold)
        {
            return;
        }

        isHoldingDuration = true;
        heldDurationMidiNote = midiNote;
        currentHeldDuration = TimeSpan.Zero;
        durationHoldStartedTimestamp = Stopwatch.GetTimestamp();
        var version = ++durationHoldVersion;
        feedbackKey = "DurationHoldHoldingFeedback";
        feedbackArguments = [requestedDurationBeats];
        feedbackClass = "feedback";
        await Audio.StartSustainedNoteAsync();
        await Audio.PlayMidiNoteAsync(72);
        _ = TrackHeldDuration(version);
    }

    private async Task HandleDurationKeyDown(KeyboardEventArgs args)
    {
        if (args.Code == "Space" || args.Key is " " or "Spacebar")
        {
            await StartDurationHold();
        }
    }

    private async Task HandleDurationKeyUp(KeyboardEventArgs args)
    {
        if (args.Code == "Space" || args.Key is " " or "Spacebar")
        {
            await EndDurationHold();
        }
    }

    private async Task TrackHeldDuration(int version)
    {
        var nextMetronomeBeat = 1;
        while (version == durationHoldVersion && isHoldingDuration && mode == DrillMode.HoldDuration)
        {
            currentHeldDuration = Stopwatch.GetElapsedTime(durationHoldStartedTimestamp);
            if (currentHeldDuration >= BeatInterval * nextMetronomeBeat)
            {
                await Audio.PlayMidiNoteAsync(72);
                nextMetronomeBeat++;
            }
            await InvokeAsync(StateHasChanged);
            await Task.Delay(50);
        }
    }

    private async Task EndDurationHold(int? midiNote = null)
    {
        if (!isHoldingDuration || heldDurationMidiNote != midiNote)
        {
            return;
        }

        currentHeldDuration = Stopwatch.GetElapsedTime(durationHoldStartedTimestamp);
        isHoldingDuration = false;
        heldDurationMidiNote = null;
        var version = ++durationHoldVersion;
        await Audio.StopSustainedNoteAsync();

        var result = HeldDurationEvaluator.Evaluate(
            requestedDurationBeats,
            currentHeldDuration,
            BeatInterval);
        await RecordDurationResult(result);
        await InvokeAsync(StateHasChanged);
        await Task.Delay(1_200);

        if (version == durationHoldVersion && mode == DrillMode.HoldDuration)
        {
            PrepareDurationRound();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task RecordDurationResult(HeldDurationResult result)
    {
        var wasRhythmEchoUnlocked = IsModeUnlocked(DrillMode.RhythmEcho);
        var wasWrittenMeasureUnlocked = IsModeUnlocked(DrillMode.WrittenMeasureTwoFour);
        var updatedDrillProgress = practiceMode == PracticeMode.LearningPath
            ? UpdateCurrentDrillProgress(result.IsSuccessful)
            : progress.DrillProgress;

        progress = progress with
        {
            Attempts = progress.Attempts + 1,
            CorrectAnswers = progress.CorrectAnswers + (result.IsSuccessful ? 1 : 0),
            Streak = result.IsSuccessful ? progress.Streak + 1 : 0,
            DrillProgress = updatedDrillProgress
        };

        feedbackKey = result.Guidance switch
        {
            HeldDurationGuidance.TooShort => "DurationHoldTooShortFeedback",
            HeldDurationGuidance.TooLong => "DurationHoldTooLongFeedback",
            _ => "DurationHoldCorrectFeedback"
        };
        feedbackArguments = [requestedDurationBeats];
        feedbackClass = result.IsSuccessful ? "feedback is-correct" : "feedback is-missed";

        if (practiceMode == PracticeMode.LearningPath &&
            result.IsSuccessful &&
            !wasRhythmEchoUnlocked &&
            IsModeUnlocked(DrillMode.RhythmEcho))
        {
            feedbackKey = "LevelUnlockedFeedback";
            feedbackArguments = [Localizer[GetModeLabelKey(DrillMode.RhythmEcho)]];
            await AwardUnlock(DrillMode.RhythmEcho);
        }

        if (practiceMode == PracticeMode.LearningPath &&
            result.IsSuccessful &&
            !wasWrittenMeasureUnlocked &&
            IsModeUnlocked(DrillMode.WrittenMeasureTwoFour))
        {
            feedbackKey = "LevelUnlockedFeedback";
            feedbackArguments = [Localizer[GetModeLabelKey(DrillMode.WrittenMeasureTwoFour)]];
            await AwardUnlock(DrillMode.WrittenMeasureTwoFour);
        }

        await ProgressStore.SaveAsync(progress);
    }
}
