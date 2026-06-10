// HealthCareApp theme switcher.
// Stored in localStorage as 'bo-primary' (hex like '#7C3AED').
(function () {
    const STORAGE_KEY = 'bo-primary';

    function darken(hex, amount) {
        // amount in [0..1]; 0.15 = 15% darker.
        const h = hex.replace('#', '');
        const r = Math.max(0, Math.min(255, parseInt(h.substring(0, 2), 16) * (1 - amount)));
        const g = Math.max(0, Math.min(255, parseInt(h.substring(2, 4), 16) * (1 - amount)));
        const b = Math.max(0, Math.min(255, parseInt(h.substring(4, 6), 16) * (1 - amount)));
        return '#' + [r, g, b].map(v => Math.round(v).toString(16).padStart(2, '0')).join('').toUpperCase();
    }

    function softTint(hex, alpha) {
        const h = hex.replace('#', '');
        const r = parseInt(h.substring(0, 2), 16);
        const g = parseInt(h.substring(2, 4), 16);
        const b = parseInt(h.substring(4, 6), 16);
        // mix toward white
        const mix = c => Math.round(c + (255 - c) * (1 - alpha));
        return '#' + [mix(r), mix(g), mix(b)].map(v => v.toString(16).padStart(2, '0')).join('').toUpperCase();
    }

    function applyPrimary(hex) {
        if (!hex || !/^#[0-9a-fA-F]{6}$/.test(hex)) return;
        const root = document.documentElement;
        root.style.setProperty('--bo-primary', hex);
        root.style.setProperty('--bo-primary-strong', darken(hex, 0.15));
        root.style.setProperty('--bo-primary-emphasis', darken(hex, 0.30));
        root.style.setProperty('--bo-primary-soft', softTint(hex, 0.18));
        root.style.setProperty('--bo-primary-tint', softTint(hex, 0.08));
    }

    window.boTheme = {
        set: function (hex) {
            try { localStorage.setItem(STORAGE_KEY, hex); } catch { }
            applyPrimary(hex);
        },
        get: function () {
            try { return localStorage.getItem(STORAGE_KEY); } catch { return null; }
        },
        reset: function () {
            try { localStorage.removeItem(STORAGE_KEY); } catch { }
            // Force CSS default (Ocean teal from :root in app.css)
            const root = document.documentElement;
            root.style.removeProperty('--bo-primary');
            root.style.removeProperty('--bo-primary-strong');
            root.style.removeProperty('--bo-primary-emphasis');
            root.style.removeProperty('--bo-primary-soft');
            root.style.removeProperty('--bo-primary-tint');
        }
    };

    // Apply on load
    const saved = window.boTheme.get();
    if (saved) applyPrimary(saved);
})();
