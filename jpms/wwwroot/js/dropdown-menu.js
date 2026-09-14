// The JS half of DropdownMenu.razor: dismiss an open menu when the user presses anywhere
// outside it, or hits Escape.
//
// A Blazor component only hears events on its own markup, so "a click somewhere else on the
// page" has to be observed at the document. While a menu is open it registers here with its root
// element and a DotNetObjectReference; a pointerdown whose target is not inside that root (or an
// Escape key) calls back to close it. Pointerdown rather than click, and in the capture phase,
// so the menu is gone before whatever was pressed handles its own click — the outside press
// still does its job (opens another menu, presses a button) instead of being swallowed.
//
// Keyed by the reference's id, not the proxy object — see the same note in app-update.js: each
// interop call materialises a fresh proxy, so a Map keyed on the object never finds its entry.
window.jpmsDropdownMenu = {
    _watches: new Map(),

    _keyOf: function (dotnetRef) {
        return dotnetRef && dotnetRef._id !== undefined ? dotnetRef._id : dotnetRef;
    },

    watch: function (root, dotnetRef) {
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
        document.addEventListener('pointerdown', onPointerDown, true);
        document.addEventListener('keydown', onKeyDown, true);
        window.jpmsDropdownMenu._watches.set(window.jpmsDropdownMenu._keyOf(dotnetRef), { onPointerDown, onKeyDown });
    },

    unwatch: function (dotnetRef) {
        const key = window.jpmsDropdownMenu._keyOf(dotnetRef);
        const watch = window.jpmsDropdownMenu._watches.get(key);
        if (!watch) return;
        document.removeEventListener('pointerdown', watch.onPointerDown, true);
        document.removeEventListener('keydown', watch.onKeyDown, true);
        window.jpmsDropdownMenu._watches.delete(key);
    }
};
