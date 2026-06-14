// Kitchen Display System client helpers: sound notifications, fullscreen, audio unlock.
// WebAudio is used for the alert tones so no audio asset files are required.
window.kds = (function () {
    let muted = false;
    let ctx = null;
    let fsHandler = null;

    function audioCtx() {
        if (ctx === null) {
            const AC = window.AudioContext || window.webkitAudioContext;
            if (AC) ctx = new AC();
        }
        return ctx;
    }

    // Synthesize a short two-tone chime. `priority` uses a higher, more urgent pattern.
    function beep(priority) {
        if (muted) return;
        const ac = audioCtx();
        if (!ac) return;
        if (ac.state === 'suspended') ac.resume();

        const now = ac.currentTime;
        const notes = priority ? [880, 1175, 1480] : [660, 880];
        const step = priority ? 0.12 : 0.16;

        notes.forEach((freq, i) => {
            const osc = ac.createOscillator();
            const gain = ac.createGain();
            osc.type = 'sine';
            osc.frequency.value = freq;
            const t0 = now + i * step;
            gain.gain.setValueAtTime(0.0001, t0);
            gain.gain.exponentialRampToValueAtTime(0.25, t0 + 0.02);
            gain.gain.exponentialRampToValueAtTime(0.0001, t0 + step);
            osc.connect(gain).connect(ac.destination);
            osc.start(t0);
            osc.stop(t0 + step + 0.02);
        });
    }

    return {
        // Call from a user gesture so the browser permits audio later.
        unlockAudio: function () {
            const ac = audioCtx();
            if (ac && ac.state === 'suspended') ac.resume();
        },
        setMuted: function (value) { muted = !!value; },

        getTheme: function () { try { return localStorage.getItem('kds-theme'); } catch (e) { return null; } },
        setTheme: function (v) { try { localStorage.setItem('kds-theme', v); } catch (e) { /* ignore */ } },

        playNewOrder: function () { beep(false); },
        playPriority: function () { beep(true); },

        isFullscreen: function () { return !!document.fullscreenElement; },
        enterFullscreen: function (el) {
            const target = el || document.documentElement;
            if (target.requestFullscreen) return target.requestFullscreen();
        },
        exitFullscreen: function () {
            if (document.exitFullscreen && document.fullscreenElement) return document.exitFullscreen();
        },
        onFullscreenChange: function (dotNetRef) {
            if (fsHandler) document.removeEventListener('fullscreenchange', fsHandler);
            fsHandler = function () {
                dotNetRef.invokeMethodAsync('OnFullscreenChanged', !!document.fullscreenElement);
            };
            document.addEventListener('fullscreenchange', fsHandler);
        },
        dispose: function () {
            if (fsHandler) { document.removeEventListener('fullscreenchange', fsHandler); fsHandler = null; }
        }
    };
})();
