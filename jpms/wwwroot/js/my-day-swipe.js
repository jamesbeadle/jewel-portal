/**
 * A horizontal swipe on a phone, reported as "left" or "right" to the component that owns the
 * element. My day's week pages with it: a swipe to the left goes forward a week, to the right
 * back one. A touch that drifts more up or down than across is the page scrolling and is not
 * a swipe; a short one is a tap. Keyed by the reference's id, as dropdown-menu.js explains:
 * each interop call materialises a fresh proxy, so a Map keyed on the object never finds it.
 */
window.jpmsSwipe = (() => {
    const minimumDistance = 48;
    const watches = new Map();

    const keyOf = dotnetRef => (dotnetRef && dotnetRef._id !== undefined ? dotnetRef._id : dotnetRef);

    const directionOf = (start, end) => {
        const across = end.clientX - start.clientX;
        const down = end.clientY - start.clientY;
        if (Math.abs(across) < minimumDistance || Math.abs(across) < Math.abs(down)) return null;
        return across < 0 ? "left" : "right";
    };

    return {
        watch: (element, dotnetRef) => {
            window.jpmsSwipe.unwatch(dotnetRef);
            if (!element) return;
            let start = null;
            const onStart = event => { start = event.changedTouches[0]; };
            const onEnd = event => {
                if (!start) return;
                const direction = directionOf(start, event.changedTouches[0]);
                start = null;
                if (direction) dotnetRef.invokeMethodAsync("OnSwiped", direction).catch(() => { });
            };
            element.addEventListener("touchstart", onStart, { passive: true });
            element.addEventListener("touchend", onEnd, { passive: true });
            watches.set(keyOf(dotnetRef), () => {
                element.removeEventListener("touchstart", onStart);
                element.removeEventListener("touchend", onEnd);
            });
        },
        unwatch: dotnetRef => {
            const stop = watches.get(keyOf(dotnetRef));
            if (!stop) return;
            stop();
            watches.delete(keyOf(dotnetRef));
        }
    };
})();
