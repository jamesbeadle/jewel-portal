// A click on a modal's backdrop dismisses the dialog (Modal.razor, HandleOverlayClick). The catch
// is what the browser calls a click: it is delivered to the nearest element that contains BOTH the
// spot where the button went down and the spot where it came up. Start a drag inside the dialog —
// sweeping across a textarea to select a sentence, say — and let go over the backdrop, and the
// OVERLAY receives a click of its own, indistinguishable to Blazor from a deliberate tap on the
// backdrop, and the dialog vanished with everything typed into it (reported 2026-09-14).
//
// So a click on a modal overlay only counts when the press AND the release both landed on that
// overlay — the same press/release pairing the browser's own light-dismiss uses for <dialog> and
// popovers. Anything else is a drag, and is swallowed here in the capture phase, before Blazor's
// delegated listener on the document ever sees it. Every real click is preceded by its own
// pointerdown and pointerup, so the two remembered targets are always the ones for THIS click.
// Registered once for the whole document: a modal is any element carrying data-modal-overlay.
(function () {
    let pressTarget = null;
    let releaseTarget = null;

    document.addEventListener("pointerdown", event => { pressTarget = event.target; }, true);
    document.addEventListener("pointerup", event => { releaseTarget = event.target; }, true);

    document.addEventListener("click", event => {
        const overlay = event.target;
        const isModalOverlay = overlay instanceof Element && overlay.hasAttribute("data-modal-overlay");
        if (!isModalOverlay) return;

        const isClickOnBackdrop = pressTarget === overlay && releaseTarget === overlay;
        if (isClickOnBackdrop) return;

        event.stopImmediatePropagation();
    }, true);
})();
