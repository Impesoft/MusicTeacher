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
    private TimeSpan currentHeldDuration;

    private string DurationProgressStyle
    {
        get
        {
            var targetMilliseconds = BeatInterval.TotalMilliseconds * requestedDurationBeats;
            var percent = targetMilliseconds <= 0
                ? 0
                : Math.Clamp(currentHeldDuration.TotalMilliseconds / targetMilliseconds * 100, 0, 100);
            return FormattableString.Invariant($"width: {percent:0.#}%;");
        }
    }

    private string DurationProgressBeats
        => FormattableString.Invariant(
            $"{Math.Min(currentHeldDuration.TotalMilliseconds / BeatInterval.TotalMilliseconds, requestedDurationBeats):0.##}");

    private string DurationRulerStyle
        => FormattableString.Invariant($"grid-template-columns: repeat({requestedDurationBeats}, 1fr);");

    private void PrepareDurationRound()
    {
        durationHoldVersion++;
        isHoldingDuration = false;
        heldDurationMidiNote = null;
        currentHeldDuration = TimeSpan.Zero;
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

    private async Task CancelDurationHold()
    {
        durationHoldVersion++;
        isHoldingDuration = false;
        heldDurationMidiNote = null;
        currentHeldDuration = TimeSpan.Zero;
        await Audio.StopSustainedNoteAsync();
    }

    private async Task StartDurationHold(int? midiNote = null)
    {
        if (mode != DrillMode.HoldDuration || isHoldingDuration)
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
        while (version == durationHoldVersion && isHoldingDuration && mode == DrillMode.HoldDuration)
        {
            currentHeldDuration = Stopwatch.GetElapsedTime(durationHoldStartedTimestamp);
            await InvokeAsync(StateHasChanged);
            await Task.Delay(80);
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
        await ProgressStore.SaveAsync(progress);
    }
}
