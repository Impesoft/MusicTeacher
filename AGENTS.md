# MusicTeacher repository guidance

## Input and learning rules

- Treat pointer, touch, and physical MIDI keyboards as input sources for the same on-screen piano action; keep scoring logic source-neutral.
- Physical MIDI input may submit an answer only in exercises whose intended answer control is a piano key.
- Never let MIDI notes answer staff-placement exercises. MIDI may still be reflected visually there, but the learner must place the note on the staff.
- Keep MIDI optional, default it to Off, and preserve the on-screen keyboard as the universal fallback.
- Do not count out-of-range MIDI notes as wrong answers. Reflect them with neutral lower/higher range feedback.
- Keep lesson highlights and physically held-key highlights as separate visual states.

## Browser integration

- Request Web MIDI permission only in response to a deliberate user action.
- Keep browser API details in a JavaScript adapter and expose a small typed service to Blazor.
- Detach browser event listeners and clear held-note state when MIDI is turned off, disconnected, or disposed.
- Treat note-on with velocity zero as note-off. Velocity is not part of phase-one scoring.

## Verification

- Add tests whenever an input source can affect scoring, especially tests proving that staff-placement modes cannot be answered through MIDI.
- Keep English and Dutch resources in sync when adding learner-facing text.
