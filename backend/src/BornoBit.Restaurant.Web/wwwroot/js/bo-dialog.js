// BoDialog: keyboard + focus helpers.
// Keeps a stack of dialog ids so ESC always pops the topmost.
(function () {
    if (window.boDialog) return;

    const stack = [];
    let dotNetRef = null;

    function onKeyDown(e) {
        if (e.key !== 'Escape' || stack.length === 0) return;
        const top = stack[stack.length - 1];
        if (!top.dismissOnEsc) return;
        e.preventDefault();
        if (dotNetRef) dotNetRef.invokeMethodAsync('OnEscape', top.id);
    }

    document.addEventListener('keydown', onKeyDown);

    window.boDialog = {
        register(dotNet) {
            dotNetRef = dotNet;
        },
        push(id, dismissOnEsc) {
            stack.push({ id, dismissOnEsc: !!dismissOnEsc });
            // lock body scroll while any dialog is open
            document.body.style.overflow = 'hidden';
        },
        pop(id) {
            const idx = stack.findIndex(x => x.id === id);
            if (idx >= 0) stack.splice(idx, 1);
            if (stack.length === 0) {
                document.body.style.overflow = '';
            }
        },
        focusDialog(el) {
            if (!el) return;
            const focusable = el.querySelector(
                'input:not([disabled]),select:not([disabled]),textarea:not([disabled]),button:not([disabled]),[tabindex]:not([tabindex="-1"])'
            );
            if (focusable) {
                try { focusable.focus(); } catch { /* noop */ }
            } else {
                try { el.focus(); } catch { /* noop */ }
            }
        }
    };
})();
