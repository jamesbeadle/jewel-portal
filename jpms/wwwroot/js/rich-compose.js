// Rich-lite compose surface for the triage composer (RichTextEditor.razor).
//
// A contenteditable div with four behaviours the plain textarea couldn't give the PM team:
//   • basic formatting (bold / italic / lists / text colour) via execCommand — deprecated but
//     universally supported, and the server re-sanitises everything to a small allowlist anyway;
//   • PASTED IMAGES: an image on the clipboard (a screenshot, a snip of a drawing) is inserted
//     inline as a data: URL <img>. The server extracts each one into a proper cid inline
//     attachment before sending, so what the composer shows is what the recipient sees;
//   • pasted HTML is flattened to the same small tag set client-side, so Outlook's kilobytes of
//     span-soup never enter the editor (the server sanitises again regardless — this is for the
//     editing experience, not for safety);
//   • AUTO-CAPITALISATION as you type — sentence starts and the pronoun "i" — handled by
//     rich-compose-autocapitalise.js (with rich-compose-caret.js underneath), wired up here.
//
// Interop surface: init(element, dotNetRef) wires input+paste and pushes HTML changes up via
// dotNetRef.invokeMethodAsync('OnEditorHtmlChanged', html); setHtml/getHtml/clear do what they
// say; exec runs a formatting command, execValue one that takes an argument (text colour);
// dispose detaches.
window.jpmsRichCompose = (function () {
    const KEEP_TAGS = new Set([
        "P", "DIV", "BR", "B", "STRONG", "I", "EM", "U", "S",
        "UL", "OL", "LI", "A", "BLOCKQUOTE", "SPAN", "PRE", "CODE", "IMG",
        "H1", "H2", "H3", "H4", "TABLE", "THEAD", "TBODY", "TR", "TH", "TD", "HR"
    ]);
    const KEEP_ATTRS = { A: ["href"], IMG: ["src", "alt"] };
    // Elements whose CONTENT is not prose and must go with them, not be unwrapped. Outlook and
    // Word put their CSS in <style><!-- … --></style>: inside a raw-text element that "<!--" is
    // text, not a comment node, so unwrapping <style> (the default for a disallowed tag) would
    // drop the whole stylesheet into the editor as visible text — which is exactly what happened
    // when a Word-authored email was copied out of the reading pane.
    const DROP_TAGS = new Set([
        "STYLE", "SCRIPT", "HEAD", "META", "LINK", "TITLE", "TEMPLATE", "NOSCRIPT", "XML",
        "OBJECT", "EMBED", "IFRAME"
    ]);
    // Style properties that survive the paste flattener, per tag. Colour only — it matches what
    // the toolbar's colour button produces and what the server's outbound sanitiser lets through;
    // everything else in a pasted style attribute (fonts, sizes, margins) is still dropped.
    const KEEP_STYLE_PROPS = { SPAN: ["color"] };

    const instances = new Map();

    function cleanNode(node) {
        // Walk a detached tree, unwrapping disallowed elements (keeping their children) and
        // stripping every attribute not explicitly kept for that tag.
        const children = Array.from(node.childNodes);
        for (const child of children) {
            if (child.nodeType === Node.ELEMENT_NODE) {
                if (DROP_TAGS.has(child.tagName)) {
                    node.removeChild(child);
                    continue;
                }
                cleanNode(child);
                if (!KEEP_TAGS.has(child.tagName)) {
                    while (child.firstChild) node.insertBefore(child.firstChild, child);
                    node.removeChild(child);
                } else {
                    const keep = KEEP_ATTRS[child.tagName] || [];
                    // Capture the allowed style properties (if any) before the style attribute is
                    // stripped with the rest, then re-apply just those.
                    const keepStyles = KEEP_STYLE_PROPS[child.tagName] || [];
                    const preserved = keepStyles
                        .map(prop => {
                            const value = child.style.getPropertyValue(prop);
                            return value ? `${prop}: ${value}` : null;
                        })
                        .filter(Boolean)
                        .join("; ");
                    for (const attr of Array.from(child.attributes)) {
                        if (!keep.includes(attr.name.toLowerCase())) child.removeAttribute(attr.name);
                    }
                    if (preserved) child.setAttribute("style", preserved);
                    // Images may only be data: (pasted) or cid: (already inline) or https.
                    if (child.tagName === "IMG") {
                        const src = child.getAttribute("src") || "";
                        if (!/^(data:image\/|cid:|https:)/i.test(src)) child.remove();
                    }
                    if (child.tagName === "A") {
                        const href = child.getAttribute("href") || "";
                        if (!/^(https?:|mailto:)/i.test(href)) child.removeAttribute("href");
                    }
                }
            } else if (child.nodeType === Node.COMMENT_NODE) {
                node.removeChild(child);
            }
        }
    }

    function notify(element) {
        const entry = instances.get(element);
        if (entry) entry.dotNetRef.invokeMethodAsync("OnEditorHtmlChanged", element.innerHTML);
    }

    function insertHtmlAtCaret(element, html) {
        element.focus();
        // execCommand("insertHTML") keeps the caret position and undo stack.
        document.execCommand("insertHTML", false, html);
        notify(element);
    }

    function onPaste(event) {
        const element = event.currentTarget;
        const clipboard = event.clipboardData;
        if (!clipboard) return;

        // Images first — a screenshot paste usually carries BOTH an image item and an HTML
        // fragment pointing at nothing; the image is what the user meant.
        for (const item of clipboard.items) {
            if (item.kind === "file" && item.type.startsWith("image/")) {
                event.preventDefault();
                const file = item.getAsFile();
                if (!file) return;
                const reader = new FileReader();
                reader.onload = () => {
                    insertHtmlAtCaret(element,
                        `<img src="${reader.result}" alt="pasted image" style="max-width:100%">`);
                };
                reader.readAsDataURL(file);
                return;
            }
        }

        const html = clipboard.getData("text/html");
        if (html) {
            event.preventDefault();
            // Parse as a whole document rather than innerHTML on a div: the clipboard carries a
            // full <html><head>…</head><body>…</body></html> from Outlook/Word, and a document
            // parser puts the head's <style>/<meta> where they belong instead of inlining them
            // ahead of the body as element children.
            const parsed = new DOMParser().parseFromString(html, "text/html");
            const scratch = parsed.body;
            cleanNode(scratch);
            insertHtmlAtCaret(element, scratch.innerHTML);
            return;
        }
        // Plain text: let the browser handle it (contenteditable inserts it as text).
    }

    return {
        init: function (element, dotNetRef) {
            if (!element) return;
            const onInput = () => notify(element);
            element.addEventListener("input", onInput);
            element.addEventListener("paste", onPaste);
            element.addEventListener("beforeinput", window.jpmsAutoCapitalise.onBeforeInput);
            element.addEventListener("keydown", window.jpmsAutoCapitalise.onKeyDown);
            instances.set(element, { dotNetRef, onInput });
        },
        setHtml: function (element, html) {
            if (element) element.innerHTML = html || "";
        },
        getHtml: function (element) {
            return element ? element.innerHTML : "";
        },
        clear: function (element) {
            if (element) element.innerHTML = "";
        },
        exec: function (element, command) {
            if (!element) return;
            element.focus();
            // styleWithCSS off so bold/italic/underline keep producing <b>/<i>/<u>, which is what
            // both sanitisers' tag allowlists expect (styleWithCSS is document-wide state, so an
            // earlier execValue call would otherwise leak into these).
            document.execCommand("styleWithCSS", false, false);
            document.execCommand(command, false, null);
            notify(element);
        },
        execValue: function (element, command, value) {
            if (!element) return;
            element.focus();
            // styleWithCSS on so foreColor produces <span style="color:…"> rather than <font>,
            // which is the one styled form the sanitisers allow through.
            document.execCommand("styleWithCSS", false, true);
            document.execCommand(command, false, value);
            document.execCommand("styleWithCSS", false, false);
            notify(element);
        },
        dispose: function (element) {
            const entry = instances.get(element);
            if (entry && element) {
                element.removeEventListener("input", entry.onInput);
                element.removeEventListener("paste", onPaste);
                element.removeEventListener("beforeinput", window.jpmsAutoCapitalise.onBeforeInput);
                element.removeEventListener("keydown", window.jpmsAutoCapitalise.onKeyDown);
            }
            instances.delete(element);
        }
    };
})();
