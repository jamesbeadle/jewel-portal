// Pinned horizontal scrollbar for wide tables (used by FinancialsTable.razor).
// The table's scroll container can be up to 85vh tall, so its own horizontal
// scrollbar — drawn at the container's bottom edge — usually sits below the
// fold. A thin proxy scroller rendered after the table sticks to the viewport
// bottom instead, and the two scrollLeft values are kept in sync both ways, so
// the table can be scrolled sideways without first scrolling the page down.
// The proxy shows only while it earns its place: the table actually overflows
// horizontally, part of it is on screen, and the table's own scrollbar (its
// bottom edge, watched via the sentinel element) is out of view.
window.jpmsStickyScroll = (() => {
    const states = new Map();

    const refresh = state => {
        const { scroller, proxy, spacer } = state;
        // The spacer mirrors the table's scrollable width, so the proxy's thumb
        // has the same proportions and range as the real scrollbar.
        spacer.style.width = scroller.scrollWidth + "px";
        const overflows = scroller.scrollWidth > scroller.clientWidth + 1;
        const show = overflows && state.tableOnScreen && !state.bottomOnScreen;
        proxy.style.display = show ? "block" : "none";
        if (show) proxy.scrollLeft = scroller.scrollLeft;
    };

    return {
        init: (scroller, sentinel, proxy, spacer) => {
            if (!scroller || states.has(scroller)) return;
            const state = { scroller, proxy, spacer, tableOnScreen: false, bottomOnScreen: true };

            // Mirror scroll positions. Assigning an unchanged scrollLeft fires no
            // scroll event, so the pair settles instead of ping-ponging.
            state.onScrollerScroll = () => { proxy.scrollLeft = scroller.scrollLeft; };
            state.onProxyScroll = () => { scroller.scrollLeft = proxy.scrollLeft; };
            scroller.addEventListener("scroll", state.onScrollerScroll, { passive: true });
            proxy.addEventListener("scroll", state.onProxyScroll, { passive: true });

            // Table or container width changes (data arriving, hide-zero toggle,
            // window resize) re-size the spacer and re-evaluate visibility.
            state.resize = new ResizeObserver(() => refresh(state));
            state.resize.observe(scroller);
            if (scroller.firstElementChild) state.resize.observe(scroller.firstElementChild);

            state.intersect = new IntersectionObserver(entries => {
                for (const entry of entries) {
                    if (entry.target === scroller) state.tableOnScreen = entry.isIntersecting;
                    else state.bottomOnScreen = entry.isIntersecting;
                }
                refresh(state);
            });
            state.intersect.observe(scroller);
            state.intersect.observe(sentinel);

            states.set(scroller, state);
            refresh(state);
        },

        dispose: scroller => {
            const state = states.get(scroller);
            if (!state) return;
            state.scroller.removeEventListener("scroll", state.onScrollerScroll);
            state.proxy.removeEventListener("scroll", state.onProxyScroll);
            state.resize.disconnect();
            state.intersect.disconnect();
            states.delete(scroller);
        }
    };
})();
