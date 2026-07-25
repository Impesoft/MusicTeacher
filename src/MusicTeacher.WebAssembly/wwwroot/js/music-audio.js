(function () {
  let audioContext;

  function getAudioContext() {
    audioContext ||= new (window.AudioContext || window.webkitAudioContext)();

    if (audioContext.state === "suspended") {
      void audioContext.resume();
    }

    return audioContext;
  }

  function playTone(frequency, duration, gainValue, waveType = "sine") {
    const context = getAudioContext();
    const now = context.currentTime;
    const oscillator = context.createOscillator();
    const gain = context.createGain();

    oscillator.type = waveType;
    oscillator.frequency.setValueAtTime(frequency, now);

    gain.gain.setValueAtTime(0.0001, now);
    gain.gain.exponentialRampToValueAtTime(gainValue, now + 0.025);
    gain.gain.exponentialRampToValueAtTime(0.0001, now + duration);

    oscillator.connect(gain);
    gain.connect(context.destination);

    oscillator.start(now);
    oscillator.stop(now + duration + 0.03);
  }

  window.musicTeacherAudio = {
    playNote(frequency) {
      playTone(frequency, 0.32, 0.045, "sine");
    },
    playNoteForDuration(frequency, durationSeconds) {
      const safeDuration = Math.min(Math.max(durationSeconds, 0.1), 4);
      playTone(frequency, safeDuration, 0.045, "sine");
    },
    playBuzzer() {
      const context = getAudioContext();
      const now = context.currentTime;
      const oscillator = context.createOscillator();
      const gain = context.createGain();

      oscillator.type = "sawtooth";
      oscillator.frequency.setValueAtTime(130, now);
      oscillator.frequency.exponentialRampToValueAtTime(82, now + 0.18);

      gain.gain.setValueAtTime(0.0001, now);
      gain.gain.exponentialRampToValueAtTime(0.05, now + 0.015);
      gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.2);

      oscillator.connect(gain);
      gain.connect(context.destination);

      oscillator.start(now);
      oscillator.stop(now + 0.23);
    }
  };
})();
