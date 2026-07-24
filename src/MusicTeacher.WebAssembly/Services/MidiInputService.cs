using Microsoft.JSInterop;

namespace MusicTeacher.WebAssembly.Services;

public sealed class MidiInputService(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private IJSObjectReference? module;
    private DotNetObjectReference<MidiInputService>? callbackReference;

    public bool IsSupported { get; private set; }
    public bool AccessFailed { get; private set; }
    public IReadOnlyList<MidiInputDevice> Devices { get; private set; } = [];
    public string? SelectedDeviceId { get; private set; }
    public bool IsListening { get; private set; }
    public string? ListeningError { get; private set; }
    public int? LastMidiNote { get; private set; }
    public int MessageCount { get; private set; }
    public MidiBrowserDiagnostics? BrowserDiagnostics { get; private set; }

    public event Func<MidiNoteChange, Task>? NoteChanged;
    public event Action? DevicesChanged;
    public event Action? DiagnosticsChanged;

    public async Task InitializeAsync()
    {
        module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/midi-input.js");
        callbackReference ??= DotNetObjectReference.Create(this);
        IsSupported = await module.InvokeAsync<bool>("isSupported");
    }

    public async Task RequestAccessAsync()
    {
        await InitializeAsync();
        if (!IsSupported) return;

        try
        {
            var devices = await module!.InvokeAsync<MidiInputDevice[]>("requestAccess", callbackReference);
            AccessFailed = false;
            UpdateDevices(devices);
        }
        catch (JSException)
        {
            AccessFailed = true;
            UpdateDevices([]);
        }
    }

    public async Task SelectDeviceAsync(string? deviceId)
    {
        await InitializeAsync();
        SelectedDeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId;
        LastMidiNote = null;
        MessageCount = 0;
        await module!.InvokeVoidAsync("selectInput", SelectedDeviceId);
        DiagnosticsChanged?.Invoke();
    }

    public async Task RefreshDiagnosticsAsync()
    {
        if (module is null) return;
        BrowserDiagnostics = await module.InvokeAsync<MidiBrowserDiagnostics>("getDiagnostics");
        DiagnosticsChanged?.Invoke();
    }

    [JSInvokable]
    public Task OnMidiMessage(int midiNote, bool isPressed)
    {
        LastMidiNote = midiNote;
        MessageCount++;
        DiagnosticsChanged?.Invoke();
        return NotifyNoteChanged(new MidiNoteChange(midiNote, isPressed));
    }

    [JSInvokable]
    public void OnMidiDevicesChanged(MidiInputDevice[] devices) => UpdateDevices(devices);

    [JSInvokable]
    public void OnMidiListeningChanged(bool isListening, string? error)
    {
        IsListening = isListening;
        ListeningError = error;
        DiagnosticsChanged?.Invoke();
    }

    private async Task NotifyNoteChanged(MidiNoteChange change)
    {
        if (NoteChanged is null) return;

        foreach (var handler in NoteChanged.GetInvocationList().Cast<Func<MidiNoteChange, Task>>())
        {
            await handler(change);
        }
    }

    private void UpdateDevices(IReadOnlyList<MidiInputDevice> devices)
    {
        Devices = devices;
        DevicesChanged?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            try
            {
                await module.InvokeVoidAsync("dispose");
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        callbackReference?.Dispose();
    }
}

public sealed record MidiInputDevice(string Id, string Name, string State);
public readonly record struct MidiNoteChange(int MidiNote, bool IsPressed);
public sealed record MidiBrowserDiagnostics(
    int RawMessageCount,
    MidiRawMessage? LastRawMessage,
    string? CallbackError,
    string? SelectedInputId,
    string? SelectedInputState,
    string? SelectedInputConnection);
public sealed record MidiRawMessage(string? DeviceId, int Status, int Data1, int Data2);
