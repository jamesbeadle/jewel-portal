// Scrolls an element into view and flashes it, so "go to" clicks that land on the
// page you are already on (e.g. the lineage bar's VO chip on the VOQ page) give
// visible feedback instead of appearing to do nothing.
window.jpmsFocusElement = function (id) {
    const element = document.getElementById(id);
    if (!element) return;
    element.scrollIntoView({ behavior: "smooth", block: "center" });
    // Restart the animation if the class is already there (repeated clicks).
    element.classList.remove("jpms-flash");
    void element.offsetWidth; // force reflow so the animation replays
    element.classList.add("jpms-flash");
    setTimeout(() => element.classList.remove("jpms-flash"), 1600);
};
