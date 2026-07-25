using Microsoft.JSInterop;
using MusicTeacher.Shared.MusicTheory;

namespace MusicTeacher.WebAssembly.Services;

public sealed class MusicAudioService(IJSRuntime jsRuntime)
{
    public ValueTask PlayNoteAsync(Pitch pitch)
        => jsRuntime.InvokeVoidAsync("musicTeacherAudio.playNote", pitch.FrequencyHz);

    public ValueTask PlayMidiNoteAsync(int midiNote)
    {
        if (midiNote is < 0 or > 127)
        {
            throw new ArgumentOutOfRangeException(nameof(midiNote));
        }

        var frequencyHz = 440d * Math.Pow(2d, (midiNote - 69) / 12d);
        return jsRuntime.InvokeVoidAsync("musicTeacherAudio.playNote", frequencyHz);
    }

    public ValueTask PlayBuzzerAsync()
        => jsRuntime.InvokeVoidAsync("musicTeacherAudio.playBuzzer");
}
