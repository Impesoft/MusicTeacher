let midiAccess = null;
let selectedInput = null;
let selectedInputId = null;
let callback = null;
let rawMessageCount = 0;
let lastRawMessage = null;
let callbackError = null;

export function isSupported() {
    return typeof navigator.requestMIDIAccess === "function";
}

export async function requestAccess(dotNetCallback) {
    callback = dotNetCallback;
    if (!isSupported()) return [];

    if (!midiAccess) {
        midiAccess = await navigator.requestMIDIAccess({ sysex: false });
        midiAccess.onstatechange = notifyDevicesChanged;
    }

    return getInputs();
}

export async function selectInput(deviceId) {
    detachSelectedInput();
    selectedInputId = deviceId || null;
    rawMessageCount = 0;
    lastRawMessage = null;
    callbackError = null;
    if (!midiAccess || !deviceId) {
        await notifyListeningChanged(false, null);
        return;
    }

    await attachSelectedInput();
}

function onMidiMessage(event) {
    const [status, note, velocity] = event.data;
    const command = status & 0xf0;
    rawMessageCount++;
    lastRawMessage = {
        deviceId: event.currentTarget?.id || selectedInputId,
        status,
        data1: note,
        data2: velocity
    };

    if (command === 0x90) {
        sendMidiMessage(note, velocity > 0);
    } else if (command === 0x80) {
        sendMidiMessage(note, false);
    }
}

function sendMidiMessage(note, isPressed) {
    callback?.invokeMethodAsync("OnMidiMessage", note, isPressed)
        .then(() => callbackError = null)
        .catch(error => callbackError = error?.message || String(error));
}

export function getDiagnostics() {
    return {
        rawMessageCount,
        lastRawMessage,
        callbackError,
        selectedInputId,
        selectedInputState: selectedInput?.state ?? null,
        selectedInputConnection: selectedInput?.connection ?? null
    };
}

async function notifyDevicesChanged() {
    if (selectedInput && selectedInput.state !== "connected") detachSelectedInput();
    if (!selectedInput && selectedInputId) await attachSelectedInput();
    callback?.invokeMethodAsync("OnMidiDevicesChanged", getInputs());
}

async function attachSelectedInput() {
    const input = midiAccess?.inputs.get(selectedInputId) ?? null;
    if (!input || input.state !== "connected") {
        selectedInput = null;
        await notifyListeningChanged(false, "disconnected");
        return;
    }

    try {
        await input.open();
        selectedInput = input;
        selectedInput.addEventListener("midimessage", onMidiMessage);
        await notifyListeningChanged(true, null);
    } catch (error) {
        selectedInput = null;
        await notifyListeningChanged(false, error?.message || "open-failed");
    }
}

function notifyListeningChanged(isListening, error) {
    return callback?.invokeMethodAsync("OnMidiListeningChanged", isListening, error) ?? Promise.resolve();
}

function getInputs() {
    if (!midiAccess) return [];

    return Array.from(midiAccess.inputs.values()).map(input => ({
        id: input.id,
        name: input.name || "MIDI keyboard",
        state: input.state
    }));
}

function detachSelectedInput() {
    if (!selectedInput) return;
    selectedInput.removeEventListener("midimessage", onMidiMessage);
    selectedInput = null;
}

export function dispose() {
    detachSelectedInput();
    selectedInputId = null;
    if (midiAccess) midiAccess.onstatechange = null;
    callback = null;
}
