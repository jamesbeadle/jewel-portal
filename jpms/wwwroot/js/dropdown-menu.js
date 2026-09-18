// The one definition of "a press outside closes it" on this site. DropdownMenu.razor is its
// first consumer; SearchSelect.razor is its second, and is deliberately NOT a DropdownMenu —
// it is a form control that shares this watcher and nothing else.
//
// A Blazor component only hears events on its own markup, so "a click somewhere else on the
// page" has to be observed at the document. While a menu is open it registers here with its root
// element and a DotNetObjectReference; a pointerdown whose target is not inside that root (or an
// Escape key) calls back to close it. Pointerdown rather than click, and in the capture phase,
// so the menu is gone before whatever was pressed handles its own click — the outside press
// still does its job (opens another menu, presses a button) instead of being swallowed.
//
// A popup that is position:fixed does not travel with the container it sits in, so once anything
// scrolls it points at nothing. Such a consumer asks for shouldCloseOnScroll; one whose panel is
// absolutely positioned moves with its container and must not, or it would close on its own
// scrolling. Passing it is safe against a stale cached shell, which simply ignores the argument.
//
// Keyed by the reference's id, not the proxy object — see the same note in app-update.js: each
// interop call materialises a fresh proxy, so a Map keyed on the object never finds its entry.
window.jpmsDropdownMenu = {
    _watches: new Map(),

    _keyOf: function (dotnetRef) {
        return dotnetRef && dotnetRef._id !== undefined ? dotnetRef._id : dotnetRef;
    },

    // Where a panel should be drawn. A panel that hangs inside its toggle's box is clipped by
    // any scrolling ancestor — a long register, a modal body — so it is drawn against the
    // viewport instead and positioned from the toggle's own rect. The caller decides which edges
    // to pin; this only reports, so the same reading serves a menu and a typeahead popup.
    measure: function (toggle) {
        const r = toggle.getBoundingClientRect();
        return {
            top: r.top, left: r.left, right: r.right, bottom: r.bottom, width: r.width,
            viewportWidth: window.innerWidth, viewportHeight: window.innerHeight
        };
    },

    watch: function (root, dotnetRef, shouldCloseOnScroll) {
        window.jpmsDropdownMenu.unwatch(dotnetRef);
        if (!root) return;
        const close = () => dotnetRef.invokeMethodAsync('CloseFromOutside').catch(() => { });
        const onPointerDown = e => {
            const path = typeof e.composedPath === 'function' ? e.composedPath() : [];
            if (path.length ? path.includes(root) : root.contains(e.target)) return;
            close();
        };
        const onKeyDown = e => {
            if (e.key === 'Escape') close();
        };
        const onScroll = shouldCloseOnScroll ? () => close() : null;
        document.addEventListener('pointerdown', onPointerDown, true);
        document.addEventListener('keydown', onKeyDown, true);
        if (onScroll) {
            document.addEventListener('scroll', onScroll, true);
            window.addEventListener('resize', onScroll);
        }
        window.jpmsDropdownMenu._watches.set(window.jpmsDropdownMenu._keyOf(dotnetRef), { onPointerDown, onKeyDown, onScroll });
    },

    unwatch: function (dotnetRef) {
        const key = window.jpmsDropdownMenu._keyOf(dotnetRef);
        const watch = window.jpmsDropdownMenu._watches.get(key);
        if (!watch) return;
        document.removeEventListener('pointerdown', watch.onPointerDown, true);
        document.removeEventListener('keydown', watch.onKeyDown, true);
        if (watch.onScroll) {
            document.removeEventListener('scroll', watch.onScroll, true);
            window.removeEventListener('resize', watch.onScroll);
        }
        window.jpmsDropdownMenu._watches.delete(key);
    }
};
