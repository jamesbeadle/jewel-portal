// Enter sends, Shift+Enter breaks the line — the composer keys every chat app has taught people.
// Blazor's @onkeydown handler on the textarea already calls Send for a plain Enter, but Blazor
// cannot preventDefault for ONE key without swallowing every key, so the browser still inserted a
// newline alongside the send. This listener sits on the panel (the composer is re-rendered after
// the billed-usage notice, so the element itself is not a stable target) and stops only that
// default; the Blazor handler runs regardless of defaultPrevented and does the sending.
window.jpmsChatComposerKeys = function (zoneId) {
    const zone = document.getElementById(zoneId);
    if (!zone) return false;                       // panel not rendered yet — caller retries
    if (zone.dataset.jpmsComposerKeysWired === "1") return true;
    zone.dataset.jpmsComposerKeysWired = "1";

    zone.addEventListener("keydown", (e) => {
        if (e.key !== "Enter" || e.shiftKey) return;
        if (!(e.target instanceof HTMLTextAreaElement)) return;
        e.preventDefault();
    });

    return true;
};
