// Helpers for the SearchSelect component: the popup is position:fixed (to escape
// overflow-clipping containers like the allocation table and modal bodies), so
// it needs the control's viewport rect; and keyboard navigation needs to keep
// the highlighted item scrolled into view.
window.jpmsSearchSelect = {
    rect: element => {
        const r = element.getBoundingClientRect();
        return { top: r.top, left: r.left, width: r.width, bottom: r.bottom, viewportHeight: window.innerHeight, viewportWidth: window.innerWidth };
    },
    reveal: id => document.getElementById(id)?.scrollIntoView({ block: "nearest" })
};
