// The Control Centre workspace's browser-side pair of hands (Features/Triage/Workspace/
// PanelWorkspace.razor): dragging the divider resizes the panes live (writing the --left-w
// variable the grid template reads), reporting the final fraction to .NET only on release so
// Blazor re-renders once, not sixty times a second; and a media-query listener tells .NET when
// the viewport crosses the lg breakpoint, so pane logic (not just CSS) knows mobile from desktop.
window.panelWorkspace = (function () {
    const DESKTOP_QUERY = "(min-width: 1024px)";
    const MIN_FRACTION = 0.2;
    const MAX_FRACTION = 0.8;
    const sessions = new Map();
    let linkChannel = null;
    let linkBye = null;

    function fractionFrom(container, clientX) {
        const rect = container.getBoundingClientRect();
        if (rect.width === 0) return MIN_FRACTION;
        const fraction = (clientX - rect.left) / rect.width;
        return Math.min(MAX_FRACTION, Math.max(MIN_FRACTION, fraction));
    }

    return {
        init(container, divider, dotnetRef) {
            let dragging = false;
            let lastFraction = null;

            const onPointerDown = (event) => {
                dragging = true;
                divider.setPointerCapture(event.pointerId);
                document.body.style.userSelect = "none";
                event.preventDefault();
            };
            const onPointerMove = (event) => {
                if (!dragging) return;
                lastFraction = fractionFrom(container, event.clientX);
                container.style.setProperty("--left-w", (lastFraction * 100).toFixed(2) + "%");
            };
            const onPointerUp = () => {
                if (!dragging) return;
                dragging = false;
                document.body.style.userSelect = "";
                if (lastFraction !== null) dotnetRef.invokeMethodAsync("OnDividerMoved", lastFraction);
            };

            divider.addEventListener("pointerdown", onPointerDown);
            divider.addEventListener("pointermove", onPointerMove);
            divider.addEventListener("pointerup", onPointerUp);
            divider.addEventListener("pointercancel", onPointerUp);

            const mediaQuery = window.matchMedia(DESKTOP_QUERY);
            const onViewportChange = () => dotnetRef.invokeMethodAsync("OnViewportChanged", mediaQuery.matches);
            mediaQuery.addEventListener("change", onViewportChange);
            onViewportChange();

            sessions.set(container, () => {
                divider.removeEventListener("pointerdown", onPointerDown);
                divider.removeEventListener("pointermove", onPointerMove);
                divider.removeEventListener("pointerup", onPointerUp);
                divider.removeEventListener("pointercancel", onPointerUp);
                mediaQuery.removeEventListener("change", onViewportChange);
            });
        },

        // Outlook-style pop-out: a real browser window (not a tab), sized for a workspace.
        openPopout(url) {
            window.open(url, "_blank", "noopener,width=1280,height=860");
        },

        // ---- Cross-window link. Every Control Centre window in this browser joins one
        //      BroadcastChannel, so popouts stay live companions of the window they were born
        //      from: selections, previews and applies travel between them. The messages are
        //      opaque JSON — .NET builds and reads them; this side only carries them. ----
        linkInit(dotnetRef, popoutKind) {
            if (typeof BroadcastChannel === "undefined") return; // very old browsers: no link
            this.linkDispose();
            const channel = new BroadcastChannel("jpms-control-centre");
            channel.onmessage = (event) =>
                dotnetRef.invokeMethodAsync("OnWorkspaceBroadcast", JSON.stringify(event.data));
            linkChannel = channel;
            // A popout announces itself (and its kind) so other windows can route to it — and
            // says goodbye when it closes, so routing stops. A main window instead asks who's
            // already out there, in case it reloaded while popouts were open.
            if (popoutKind) {
                linkBye = () => channel.postMessage({ type: "bye", kind: popoutKind });
                window.addEventListener("pagehide", linkBye);
                channel.postMessage({ type: "hello", kind: popoutKind });
            }
            // Every window asks who's already out there — a reloaded main re-learns its popouts,
            // and a late-born popout learns its siblings (for routing previews between popouts).
            channel.postMessage({ type: "who" });
        },

        linkSend(json) {
            if (linkChannel) linkChannel.postMessage(JSON.parse(json));
        },

        linkDispose() {
            if (linkBye) {
                linkBye();
                window.removeEventListener("pagehide", linkBye);
                linkBye = null;
            }
            if (linkChannel) {
                linkChannel.close();
                linkChannel = null;
            }
        },

        dispose(container) {
            const cleanUp = sessions.get(container);
            if (cleanUp) cleanUp();
            sessions.delete(container);
        }
    };
})();
