// The JS half of the "new version available" flow (UpdateToast.razor + AppVersionService).
//
// Two jobs:
//
//   1. Tell the app when a backgrounded tab becomes visible again, so it can ask the API for the
//      current version — a tab nobody is navigating sends no traffic for the header check to see.
//
//   2. Refresh onto the NEW build. This is the subtle one: when the production service worker is
//      registered, it serves the whole app from its cache, and a freshly-deployed version sits in
//      a new worker that stays "waiting" until every JPMS tab has closed. A bare location.reload()
//      would be served the OLD cache by the OLD worker — the toast would reappear and the refresh
//      button would look broken. So: ask the registration to update, wait for the new worker to
//      finish installing, tell it to skip the wait (the message listener lives in
//      service-worker.published.js), wait for it to activate, and only then reload. Every step
//      degrades to a plain reload — no registration (the current setup), a slow download, an
//      unexpected state — because reloading is never the wrong thing to do here.

window.jpmsUpdate = {
    _handlers: new Map(),

    watchVisibility: function (dotnetRef) {
        const handler = () => {
            if (document.visibilityState === 'visible') dotnetRef.invokeMethodAsync('OnTabVisible');
        };
        document.addEventListener('visibilitychange', handler);
        window.jpmsUpdate._handlers.set(dotnetRef, handler);
    },

    unwatchVisibility: function (dotnetRef) {
        const handler = window.jpmsUpdate._handlers.get(dotnetRef);
        if (!handler) return;
        document.removeEventListener('visibilitychange', handler);
        window.jpmsUpdate._handlers.delete(dotnetRef);
    },

    refresh: async function () {
        try {
            const reg = navigator.serviceWorker && await navigator.serviceWorker.getRegistration();
            if (reg) {
                // Kick off the check for the new worker; ignore failure (offline check → reload).
                await reg.update().catch(() => { });

                // The new worker downloads the whole bundle during install, so give it real time —
                // this is what the button's "Updating…" state is for.
                const waiting = await new Promise(resolve => {
                    if (reg.waiting) return resolve(reg.waiting);
                    const startedAt = Date.now();
                    const poll = setInterval(() => {
                        if (reg.waiting) { clearInterval(poll); resolve(reg.waiting); }
                        else if (Date.now() - startedAt > 60000) { clearInterval(poll); resolve(null); }
                    }, 250);
                });

                if (waiting) {
                    await new Promise(resolve => {
                        waiting.addEventListener('statechange', () => {
                            if (waiting.state === 'activated') resolve();
                        });
                        waiting.postMessage('SKIP_WAITING');
                        // Belt and braces: if activation stalls, reload anyway.
                        setTimeout(resolve, 5000);
                    });
                }
            }
        } catch {
            // Fall through — a plain reload is the worst case, not a failure.
        }
        location.reload();
    }
};
