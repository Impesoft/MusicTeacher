# MusicTeacher

MusicTeacher is a standalone .NET 10 Blazor WebAssembly PWA for children learning to read music from scratch.

The current v1 focuses on treble-clef note reading with fixed-do names (`do`, `re`, `mi`, `fa`, `sol`, `la`, `ti`) across a beginner two-octave range from `C4` to `B5`.

## Current Features

- Start screen with English and Dutch language selection.
- Free explore mode where every drill is available immediately.
- Learning path mode where drills unlock through per-level streak goals.
- Reset progress action with confirmation, because resetting locks later learning-path levels again.
- Local lesson metadata loaded from JSON in `wwwroot/content/lessons`.
- Local progress storage in browser `localStorage`; no backend or account system yet.
- SVG treble staff with ledger lines for notes below and above the staff.
- SVG two-octave piano keyboard with white keys and black keys.
- Soft note playback on hover/click and buzzer feedback for incorrect answers.
- Resource-file based localization with WebAssembly globalization data enabled.

## Drill Modes

- `Name`: read the note shown on the staff and choose it on the keyboard.
- `Place`: read the requested note name and place it on the staff.
- `Hear: play`: listen to a note and play it on the keyboard.
- `Melody echo`: listen to a short phrase and repeat its pitches in order; timing is
  deliberately ignored at this stage.
- `Long melody echo`: repeat four- and five-note phrases after mastering short echoes.
- `Read a phrase`: read two- to four-note phrases from the staff and play their pitches
  in order; timing and note length are deliberately ignored.
- `Tap the beat`: follow a count-in and keep a steady pulse using touch, Space, or any MIDI key.
- `Hold the sound`: follow a four-click count-in, then press and release after one, two, or four metronome-guided beats using the hold pad, Space, or any MIDI key.
- `Rhythm echo`: listen to a four-beat sound/rest pattern and repeat its sound beats using touch, Space, or any MIDI key.
- `Hear: place`: listen to a note and place it on the staff.

The accidental selector is already present in placement drills, but sharps and flats are still disabled for the current beginner level.

## Learning Path

Learning mode uses independent streaks per drill, so practicing one level does not accidentally unlock another.

- Level 1: `Name`, streak of 5 unlocks `Place`.
- Level 2: `Place`, streak of 10 unlocks `Hear: play`.
- Level 3: `Hear: play`, streak of 5 unlocks `Hear: place`.
- Level 4: `Hear: place`, final current level.

## Project Structure

- `src/MusicTeacher.WebAssembly` - standalone Blazor WebAssembly PWA, UI, services, localization, audio, local storage.
- `src/MusicTeacher.Shared` - pure C# music-theory, lesson, and progress models.
- `tests/MusicTeacher.Tests` - xUnit unit tests plus opt-in Playwright end-to-end tests.

## Run Locally

```powershell
dotnet run --project src\MusicTeacher.WebAssembly\MusicTeacher.WebAssembly.csproj --urls http://127.0.0.1:5173
```

Then open `http://127.0.0.1:5173/`.

## Build

```powershell
dotnet build MusicTeacher.slnx --artifacts-path .artifacts
```

## Test

```powershell
dotnet test MusicTeacher.slnx --artifacts-path .artifacts
```

Playwright E2E tests are skipped by default. To run them, install Playwright browsers and enable the E2E flag:

```powershell
dotnet build MusicTeacher.slnx --artifacts-path .artifacts
pwsh .artifacts\bin\MusicTeacher.Tests\debug\playwright.ps1 install chromium
$env:MUSIC_TEACHER_RUN_E2E = "true"
dotnet test MusicTeacher.slnx --artifacts-path .artifacts
```

## Current Direction

The project intentionally stays frontend-only for v1. Add a backend only when accounts, cloud sync, teacher dashboards, or shared classroom progress become necessary.
