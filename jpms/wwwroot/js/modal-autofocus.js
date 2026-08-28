// When a modal opens, put the keyboard in it: focus the first usable control in the
// BODY (so the user can start typing straight away — e.g. the folder-name box on the
// drawings "New folder" dialog), falling back to the FOOTER's first button for
// body-less confirm dialogs. The footer renders Cancel before the action button, so
// destructive confirms land on Cancel — Enter never deletes anything by accident.
// The header's close X is deliberately never the target, and if nothing is focusable
// the panel itself takes focus (tabindex="-1") so Tab/Escape start from inside the
// dialog rather than the page behind it.
window.jpmsModalAutofocus = function (panel) {
    if (!panel || !panel.isConnected) return;
    const selector =
        "input:not([type='hidden']):not([disabled]):not([readonly]), " +
        "select:not([disabled]), textarea:not([disabled]):not([readonly]), " +
        "button:not([disabled]), a[href], [contenteditable='true'], " +
        "[tabindex]:not([tabindex='-1'])";
    const visible = el => el.getClientRects().length > 0;
    for (const scopeAttr of ["[data-modal-body]", "[data-modal-footer]"]) {
        const scope = panel.querySelector(scopeAttr);
        if (!scope) continue;
        const target = Array.from(scope.querySelectorAll(selector)).find(visible);
        if (target) {
            try { target.focus(); } catch { /* detached mid-render — next open retries */ }
            return;
        }
    }
    try { panel.focus(); } catch { /* ignore */ }
};
