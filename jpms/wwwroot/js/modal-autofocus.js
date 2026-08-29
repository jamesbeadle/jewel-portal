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
    // Never steal the keyboard mid-word (2026-08-28): a dialog that opens LATE — mounted only
    // after a slow load lands — must not yank focus from a typing surface the user has since
    // clicked into. A button/link/body as the active element means the user just clicked the
    // thing that opened this dialog, so the grab is what they expect; an active text control
    // outside the panel means they are typing somewhere else, and the courtesy backs off.
    const active = document.activeElement;
    if (active && !panel.contains(active)) {
        const typingSurface =
            active.matches?.("textarea, [contenteditable='true'], select")
            || (active.matches?.("input")
                && !/^(button|submit|reset|checkbox|radio|file|range|color|image)$/i
                    .test(active.type || "text"));
        if (typingSurface) return;
    }
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
