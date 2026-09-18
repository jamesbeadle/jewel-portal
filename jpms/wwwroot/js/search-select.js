// Helpers for the SearchSelect component: the popup is position:fixed (to escape
// overflow-clipping containers like the allocation table and modal bodies), so
// it needs the control's viewport rect; and keyboard navigation needs to keep
// the highlighted item scrolled into view.
window.jpmsSearchSelect = {
    // One reading of a control's rect for the whole site — jpmsDropdownMenu.measure. Kept as an
    // entry point of its own so a client running new assemblies against a cached shell, which
    // has this file but not that function, still positions its popup.
    rect: element => window.jpmsDropdownMenu && window.jpmsDropdownMenu.measure
        ? window.jpmsDropdownMenu.measure(element)
        : (r => ({ top: r.top, left: r.left, width: r.width, bottom: r.bottom, viewportHeight: window.innerHeight, viewportWidth: window.innerWidth }))(element.getBoundingClientRect()),
    reveal: id => document.getElementById(id)?.scrollIntoView({ block: "nearest" })
};
