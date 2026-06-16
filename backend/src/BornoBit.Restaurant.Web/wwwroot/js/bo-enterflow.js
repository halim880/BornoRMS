// bo-enterflow.js — keyboard-first form entry.
// Enter on a plain text/number input advances focus to the next control;
// Enter on the last input clicks the primary (Save) button.
// Autocomplete inputs handle their own Enter (pick highlighted option), so we skip them.
(function () {
    function focusables(root) {
        // Navigation order = visible, enabled text/number inputs + autocomplete inputs + textareas.
        // File inputs are skipped (Enter is meaningless there).
        return Array.from(root.querySelectorAll('input, textarea'))
            .filter(function (el) {
                if (el.disabled || el.readOnly) return false;
                if (el.type === 'file' || el.type === 'hidden' || el.type === 'checkbox' || el.type === 'radio') return false;
                if (el.offsetParent === null) return false; // hidden / not laid out
                return true;
            });
    }

    function isAutocompleteInput(el) {
        return el.classList && el.classList.contains('bo-cmp-ac-input');
    }

    window.boEnterFlow = {
        attach: function (root) {
            if (!root) return null;

            function onKeyDown(e) {
                if (e.key !== 'Enter' || e.shiftKey || e.isComposing) return;

                var t = e.target;
                if (!t || (t.tagName !== 'INPUT' && t.tagName !== 'TEXTAREA')) return;
                if (t.tagName === 'TEXTAREA') return;        // let Enter insert a newline
                if (isAutocompleteInput(t)) return;          // autocomplete owns its Enter

                e.preventDefault();

                var items = focusables(root);
                var idx = items.indexOf(t);
                if (idx >= 0 && idx < items.length - 1) {
                    var next = items[idx + 1];
                    next.focus();
                    if (next.select) next.select();
                } else {
                    // Last input → trigger Save.
                    var save = document.querySelector('.bo-dialog .bo-btn-primary')
                        || document.querySelector('.bo-btn-primary');
                    if (save && !save.disabled) save.click();
                }
            }

            root.addEventListener('keydown', onKeyDown);
            return {
                dispose: function () { root.removeEventListener('keydown', onKeyDown); }
            };
        }
    };
})();
