using MusicTeacher.Shared.MusicTheory;

namespace MusicTeacher.WebAssembly.Pages;

public partial class Home
{
    private IReadOnlyList<int> SelectableSteps => CurrentNotes
        .Select(TrebleClef.GetStaffStep)
        .Distinct()
        .ToArray();

    private string FeedbackText => feedbackArguments.Length == 0
        ? Localizer[feedbackKey]
        : Localizer.Format(feedbackKey, feedbackArguments);

    private bool IsPlacementMode => mode is DrillMode.PlaceNote or DrillMode.PlaceAccidental or DrillMode.HearNotePlace;

    private bool IsHearingMode => mode is DrillMode.HearNotePlay or DrillMode.HearAccidentalPlay or DrillMode.HearNotePlace;

    private bool ShowsStaff => mode is DrillMode.NameNote or DrillMode.PlaceNote or DrillMode.NameAccidental or DrillMode.PlaceAccidental or DrillMode.HearNotePlace;

    private bool ShowsKeyboard => mode is DrillMode.NameNote or DrillMode.PlaceNote or DrillMode.NameAccidental or DrillMode.PlaceAccidental or DrillMode.HearNotePlay or DrillMode.MelodyEcho or DrillMode.HearAccidentalPlay;

    private bool ShowsAccidentalSelector => mode is DrillMode.PlaceNote or DrillMode.PlaceAccidental or DrillMode.HearNotePlace;

    private bool CanChooseAccidental => mode == DrillMode.PlaceAccidental;

    private bool AcceptsMidiKeyAnswers => mode is
        DrillMode.NameNote or
        DrillMode.NameAccidental or
        DrillMode.HearNotePlay or
        DrillMode.MelodyEcho or
        DrillMode.HearAccidentalPlay;

    private Pitch? KeyboardHighlightedPitch => mode is DrillMode.PlaceNote or DrillMode.PlaceAccidental ? currentPitch : null;

    private Pitch? DisplayedPitch => mode is DrillMode.NameNote or DrillMode.NameAccidental
        ? currentPitch
        : selectedStep is null ? null : TrebleClef.GetPitchFromStaffStep(selectedStep.Value) with { Accidental = selectedAccidental };

    private string CurrentDrillTitle => mode switch
    {
        DrillMode.NameNote => Localizer["NameTheNoteTitle"],
        DrillMode.PlaceNote => Localizer["PlaceTheNoteTitle"],
        DrillMode.NameAccidental => Localizer["NameAccidentalTitle"],
        DrillMode.PlaceAccidental => Localizer["PlaceAccidentalTitle"],
        DrillMode.HearNotePlay => Localizer["HearPlayTitle"],
        DrillMode.MelodyEcho => Localizer["MelodyEchoTitle"],
        DrillMode.HearAccidentalPlay => Localizer["HearAccidentalPlayTitle"],
        DrillMode.HearNotePlace => Localizer["HearPlaceTitle"],
        _ => throw new InvalidOperationException($"Unsupported drill mode {mode}.")
    };

    private string PromptText => mode switch
    {
        DrillMode.NameNote => Localizer["NamePrompt"],
        DrillMode.PlaceNote => Localizer.Format("PlacePrompt", GetPromptName(currentPitch)),
        DrillMode.NameAccidental => Localizer["NameAccidentalPrompt"],
        DrillMode.PlaceAccidental => Localizer.Format("PlaceAccidentalPrompt", GetPromptName(currentPitch)),
        DrillMode.HearNotePlay => Localizer["HearPlayPrompt"],
        DrillMode.MelodyEcho => Localizer["MelodyEchoPrompt"],
        DrillMode.HearAccidentalPlay => Localizer["HearAccidentalPlayPrompt"],
        DrillMode.HearNotePlace => Localizer["HearPlacePrompt"],
        _ => throw new InvalidOperationException($"Unsupported drill mode {mode}.")
    };

    private IReadOnlyList<Pitch> CurrentNotes => mode switch
    {
        DrillMode.NameAccidental or DrillMode.PlaceAccidental or DrillMode.HearAccidentalPlay => TrebleClef.BeginnerAccidentalNotes,
        DrillMode.NameNote or DrillMode.HearNotePlay or DrillMode.MelodyEcho => TrebleClef.BeginnerReadingNotes,
        DrillMode.PlaceNote or DrillMode.HearNotePlace => TrebleClef.BeginnerPlacementNotes,
        _ => throw new InvalidOperationException($"Unsupported drill mode {mode}.")
    };

    private IReadOnlyList<Pitch> CurrentKeyboardPitches => TrebleClef.BeginnerReadingNotes;

    private async Task SetMode(DrillMode nextMode)
    {
        if (IsModeLocked(nextMode))
        {
            return;
        }

        mode = nextMode;
        melodyDemonstrationVersion++;
        previousPitch = null;
        NextRound();
        if (IsMelodyEchoMode)
        {
            await DemonstrateMelodyPhrase();
        }
        else
        {
            await PlayAssignmentNoteIfNeeded();
        }
    }

    private async Task SelectKeyboardPitch(Pitch pitch)
    {
        if (mode is DrillMode.PlaceNote or DrillMode.PlaceAccidental)
        {
            await PreviewPitch(pitch);
            return;
        }

        await HandlePlayedMidiNote(pitch.MidiNote);
    }

    private async Task ChoosePitch(Pitch pitch)
    {
        await HandlePlayedMidiNote(pitch.MidiNote);
    }

    private async Task HandlePlayedMidiNote(int midiNote, bool playInputSound = false)
    {
        if (playInputSound)
        {
            await Audio.PlayMidiNoteAsync(midiNote);
        }

        if (IsMelodyEchoMode)
        {
            await SubmitMelodyNote(midiNote);
            return;
        }

        var isCorrect = midiNote == currentPitch.MidiNote;
        await PlayClickCue(isCorrect, currentPitch);
        await RecordAnswer(isCorrect);
    }

    private async Task PreviewPitch(Pitch pitch)
    {
        await Audio.PlayNoteAsync(pitch);
    }

    private async Task PreviewStaffStep(int step)
    {
        await Audio.PlayNoteAsync(TrebleClef.GetPitchFromStaffStep(step) with { Accidental = selectedAccidental });
    }

    private async Task SelectStaffStep(int step)
    {
        selectedStep = step;
        var placedPitch = TrebleClef.GetPitchFromStaffStep(step) with { Accidental = selectedAccidental };
        var isExactMatch = step == TrebleClef.GetStaffStep(currentPitch) && placedPitch.Accidental == currentPitch.Accidental;
        var isEnharmonicMatch = placedPitch.IsEnharmonicEquivalentTo(currentPitch);
        var isCorrect = isEnharmonicMatch;

        await PlayClickCue(isCorrect, currentPitch);
        await RecordAnswer(isCorrect, isExactMatch ? null : placedPitch);
    }

    private async Task RecordAnswer(bool isCorrect, Pitch? placedPitch = null)
    {
        var wasNextModeUnlocked = GetNextMode(mode) is { } nextMode && IsModeUnlocked(nextMode);
        var updatedDrillProgress = practiceMode == PracticeMode.LearningPath
            ? UpdateCurrentDrillProgress(isCorrect)
            : progress.DrillProgress;

        var newGlobalStreak = isCorrect ? progress.Streak + 1 : 0;
        progress = progress with
        {
            Attempts = progress.Attempts + 1,
            CorrectAnswers = progress.CorrectAnswers + (isCorrect ? 1 : 0),
            Streak = newGlobalStreak,
            DrillProgress = updatedDrillProgress
        };

        if (isCorrect)
        {
            if (placedPitch is not null)
            {
                feedbackKey = "EnharmonicCorrectFeedback";
                feedbackArguments = [GetPromptName(currentPitch), GetPromptName(placedPitch.Value)];
            }
            else
            {
                feedbackKey = "CorrectFeedback";
                feedbackArguments = [];
            }
            feedbackClass = "feedback is-correct";
        }
        else
        {
            feedbackKey = "MissedFeedback";
            feedbackArguments = [];
            feedbackClass = "feedback is-missed";
        }

        if (practiceMode == PracticeMode.LearningPath &&
            isCorrect &&
            GetNextMode(mode) is { } unlockedMode &&
            !wasNextModeUnlocked &&
            IsModeUnlocked(unlockedMode))
        {
            feedbackKey = "LevelUnlockedFeedback";
            feedbackArguments = [Localizer[GetModeLabelKey(unlockedMode)]];
            await AwardUnlock(unlockedMode);
        }

        await ProgressStore.SaveAsync(progress);

        if (isCorrect)
        {
            var activeFeedbackKey = feedbackKey;
            var activeFeedbackArguments = feedbackArguments;
            var activeFeedbackClass = feedbackClass;

            NextRound();

            feedbackKey = activeFeedbackKey;
            feedbackArguments = activeFeedbackArguments;
            feedbackClass = activeFeedbackClass;

            await PlayAssignmentNoteIfNeeded();
        }
    }

    private async Task AdvanceRound()
    {
        melodyDemonstrationVersion++;
        NextRound();
        if (IsMelodyEchoMode)
        {
            await DemonstrateMelodyPhrase();
        }
        else
        {
            await PlayAssignmentNoteIfNeeded();
        }
    }

    private void NextRound()
    {
        if (IsMelodyEchoMode)
        {
            StartNewMelodyPhrase();
            return;
        }

        var notes = CurrentNotes;
        currentPitch = PickRandomPitch(notes);
        previousPitch = currentPitch;
        selectedStep = null;
        selectedAccidental = Accidental.Natural;
        feedbackKey = mode switch
        {
            DrillMode.NameNote => "PickNameFeedback",
            DrillMode.PlaceNote => "PickStaffFeedback",
            DrillMode.NameAccidental => "PickAccidentalNameFeedback",
            DrillMode.PlaceAccidental => "PickAccidentalStaffFeedback",
            DrillMode.HearNotePlay => "PickHeardKeyFeedback",
            DrillMode.MelodyEcho => "MelodyEchoListenFeedback",
            DrillMode.HearAccidentalPlay => "PickHeardBlackKeyFeedback",
            DrillMode.HearNotePlace => "PickHeardStaffFeedback",
            _ => throw new InvalidOperationException($"Unsupported drill mode {mode}.")
        };
        feedbackArguments = [];
        feedbackClass = "feedback";
    }

    private Pitch PickRandomPitch(IReadOnlyList<Pitch> notes)
    {
        if (notes.Count == 0)
        {
            throw new InvalidOperationException("A drill needs at least one note.");
        }

        if (notes.Count == 1 || previousPitch is null)
        {
            return notes[Random.Shared.Next(notes.Count)];
        }

        var candidates = notes.Where(note => note != previousPitch.Value).ToArray();
        return candidates[Random.Shared.Next(candidates.Length)];
    }

    private string GetModeClass(DrillMode drillMode)
    {
        var classes = new List<string> { "mode-button" };

        if (mode == drillMode)
        {
            classes.Add("is-active");
        }

        if (IsModeLocked(drillMode))
        {
            classes.Add("is-locked");
        }

        return string.Join(' ', classes);
    }

    private void SetSelectedAccidental(Accidental accidental)
    {
        if (!CanChooseAccidental)
        {
            return;
        }

        selectedAccidental = accidental;
    }

    private string GetAccidentalButtonClass(Accidental accidental)
    {
        var classes = new List<string> { "accidental-button" };

        if (selectedAccidental == accidental)
        {
            classes.Add("is-active");
        }

        if (CanChooseAccidental)
        {
            classes.Add("is-enabled");
        }

        return string.Join(' ', classes);
    }

    private async Task PlayClickCue(bool isCorrect, Pitch pitch)
    {
        if (isCorrect)
        {
            if (!IsHearingMode)
            {
                await Audio.PlayNoteAsync(pitch);
            }

            return;
        }

        await Audio.PlayBuzzerAsync();
    }

    private async Task PlayAssignmentNote()
    {
        await Audio.PlayNoteAsync(currentPitch);
    }

    private async Task PlayAssignmentNoteIfNeeded()
    {
        if (IsMelodyEchoMode)
        {
            await DemonstrateMelodyPhrase();
            return;
        }

        if (IsHearingMode)
        {
            await PlayAssignmentNote();
        }
    }

    private async Task AwardUnlock(DrillMode unlockedMode)
    {
        var modeName = Localizer[GetModeLabelKey(unlockedMode)];
        var badge = BadgeAwards.FirstOrDefault(award => award.Mode == unlockedMode)
            ?? new BadgeAward(unlockedMode, GetModeKey(unlockedMode), GetModeLabelKey(unlockedMode), GetModeLabelKey(unlockedMode), "★");

        earnedBadgeIds.Add(badge.Id);
        await SaveEarnedBadges();

        var message = unlockedMode == DrillMode.NameAccidental
            ? Localizer.Format("UnlockToastAccidentalsMessage", modeName)
            : Localizer.Format("UnlockToastMessage", modeName);

        unlockToast = new UnlockToastViewModel(
            message,
            Localizer[badge.TitleKey],
            Localizer[badge.DescriptionKey],
            badge.Icon);
    }

    private string GetPromptName(Pitch pitch)
    {
        var octaveLabel = pitch.Octave == 4 ? Localizer["LowOctave"] : Localizer["HighOctave"];
        var noteName = UseAlphabeticalNoteNames ? pitch.ScientificName : GetFixedDoName(pitch);

        return $"{octaveLabel} {noteName}";
    }

    private string GetFixedDoName(Pitch pitch)
        => Localizer.CurrentCulture.TwoLetterISOLanguageName == "nl" && pitch.Letter == NoteLetter.B
            ? $"si{GetAccidentalSymbol(pitch.Accidental)}"
            : pitch.FixedDoName;

    private static string GetAccidentalSymbol(Accidental accidental)
        => accidental switch
        {
            Accidental.Flat => "♭",
            Accidental.Natural => string.Empty,
            Accidental.Sharp => "♯",
            _ => throw new ArgumentOutOfRangeException(nameof(accidental), accidental, null)
        };

}
