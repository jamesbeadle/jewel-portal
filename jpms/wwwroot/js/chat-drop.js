// Drag-and-drop AND clipboard paste onto the assistant panel. Blazor's <InputFile> only reacts to
// drops landing on the input element itself — ours is hidden behind the paperclip, so dropping a
// file anywhere on the panel did nothing, and a pasted screenshot had nowhere to land at all.
// This makes the WHOLE panel the drop/paste target and hands the file to that same hidden input
// (files + a synthetic change event), so the one upload path — stage, then send with the
// message — handles it exactly as if the paperclip had been used.
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
        for (const file of e.dataTransfer.files) transfer.items.add(file); // every dropped file stages, same as the paperclip's multi-pick
        input.files = transfer.files;
        input.dispatchEvent(new Event("change", { bubbles: true }));
    });

    // Paste, for the screenshot on the clipboard (Cmd/Ctrl+Shift+4, Snipping Tool, a copied
    // image). Fires when focus is anywhere in the panel — the composer, usually. Only pastes that
    // CARRY a file are intercepted; ordinary text pastes fall through to the textarea untouched.
    zone.addEventListener("paste", (e) => {
        const files = e.clipboardData ? e.clipboardData.files : null;
        if (!files || files.length === 0) return;

        const input = document.getElementById(inputId);
        if (!input) return;
        e.preventDefault();

        let file = files[0]; // a clipboard carries one file; it stages ALONGSIDE anything already staged
        if (file.type && file.type.indexOf("image/") === 0) {
            // Clipboard images all arrive named "image.png" — stamp the name so two pastes into
            // one conversation read back as two attachments, not the same one twice.
            const extension = (file.type.split("/")[1] || "png").replace("jpeg", "jpg");
            const stamp = new Date().toISOString().replace(/[-:]/g, "").slice(0, 15);
            file = new File([file], "pasted-" + stamp + "." + extension, { type: file.type });
        }

        const transfer = new DataTransfer();
        transfer.items.add(file);
        input.files = transfer.files;
        input.dispatchEvent(new Event("change", { bubbles: true }));
    });

    return true;
};
