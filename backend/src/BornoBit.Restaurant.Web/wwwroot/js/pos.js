// POS page helpers. posChips reports whether the order-chip strip overflows its
// container so the page can show the "all running orders" button only when needed.
window.posChips = {
    observe: function (el, dotnetRef) {
        if (!el) return;
        const check = () => {
            try {
                dotnetRef.invokeMethodAsync('OnChipsOverflowChanged', el.scrollWidth > el.clientWidth + 1);
            } catch { /* circuit gone */ }
        };
        const ro = new ResizeObserver(check);
        ro.observe(el);
        const mo = new MutationObserver(check);
        mo.observe(el, { childList: true, subtree: true });
        el.__posChips = { ro, mo, check };
        window.addEventListener('resize', check);
        check();
    },
    dispose: function (el) {
        const s = el && el.__posChips;
        if (!s) return;
        s.ro.disconnect();
        s.mo.disconnect();
        window.removeEventListener('resize', s.check);
        delete el.__posChips;
    }
};
