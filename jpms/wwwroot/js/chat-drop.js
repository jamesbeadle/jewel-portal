// Drag-and-drop onto the assistant panel. Blazor's <InputFile> only reacts to drops landing on
// the input element itself — ours is hidden behind the paperclip, so dropping a file anywhere on
// the panel did nothing. This makes the WHOLE panel the drop target and hands the dropped file to
// that same hidden input (files + a synthetic change event), so the one upload path — stage,
// then send with the message — handles it exactly as if the paperclip had been used.
window.jpmsChatDropZone = function (zoneId, inputId) {
    const zone = document.getElementById(zoneId);
    if (!zone) return false;                       // panel not rendered yet — caller retries
    if (zone.dataset.jpmsDropWired === "1") return true;
    zone.dataset.jpmsDropWired = "1";

    // dragenter/dragleave fire for every child crossed; the depth counter stops the highlight
    // flickering off while the file is still over the panel.
    let depth = 0;

    const highlight = (on) => {
        zone.style.outline = on ? "2px dashed rgba(94, 234, 155, 0.7)" : "";
        zone.style.outlineOffset = on ? "-2px" : "";
    };

    const carriesFiles = (e) =>
        e.dataTransfer && Array.from(e.dataTransfer.types).includes("Files");

    zone.addEventListener("dragenter", (e) => {
        if (!carriesFiles(e)) return;
        e.preventDefault();
        depth++;
        highlight(true);
    });

    zone.addEventListener("dragover", (e) => {
        if (!carriesFiles(e)) return;
        e.preventDefault();                        // without this the browser opens the file
        e.dataTransfer.dropEffect = "copy";
    });

    zone.addEventListener("dragleave", () => {
        if (--depth <= 0) { depth = 0; highlight(false); }
    });

    zone.addEventListener("drop", (e) => {
        if (!carriesFiles(e)) return;
        e.preventDefault();
        depth = 0;
        highlight(false);

        // Looked up at DROP time, not wire-up time: the input only exists once the billed-usage
        // notice is accepted, and the panel outlives any one render of its composer.
        const input = document.getElementById(inputId);
        if (!input || e.dataTransfer.files.length === 0) return;

        const transfer = new DataTransfer();
        transfer.items.add(e.dataTransfer.files[0]); // one file per message, same as the paperclip
        input.files = transfer.files;
        input.dispatchEvent(new Event("change", { bubbles: true }));
    });

    return true;
};
