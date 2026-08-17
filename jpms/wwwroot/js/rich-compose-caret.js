// Caret-reading helpers for the compose editor's contenteditable body.
//
// The corrections in rich-compose-autocapitalise.js need to know what the user has typed on
// the current line so far. A contenteditable line is not a string — it is a run of text nodes,
// inline elements and the occasional <br> inside a block — so "the text before the caret" has
// to be assembled by walking backwards from the caret until something line-breaking is met.
window.jpmsComposeCaret = (function () {
    // Tags that start a new line as far as sentence detection is concerned.
    const LINE_BREAK_TAGS = new Set([
        "BR", "DIV", "P", "LI", "BLOCKQUOTE", "TD", "TH", "TR", "TABLE",
        "UL", "OL", "PRE", "HR", "H1", "H2", "H3", "H4"
    ]);
    // Sentence detection only ever inspects the tail of the line, so the backwards walk can
    // stop once this much text has been gathered.
    const ENOUGH_CONTEXT = 80;

    function isLineBreak(node) {
        if (node.nodeType !== Node.ELEMENT_NODE) return false;
        return LINE_BREAK_TAGS.has(node.tagName);
    }

    function lastLeafInside(node) {
        while (!isLineBreak(node) && node.lastChild) node = node.lastChild;
        return node;
    }

    function previousLeaf(node, editor) {
        while (node && node !== editor) {
            if (node.previousSibling) return lastLeafInside(node.previousSibling);
            node = node.parentNode;
            if (node === editor) return null;
            if (isLineBreak(node)) return null;
        }
        return null;
    }

    // The caret as a collapsed range inside this editor, or null — a spanning selection or a
    // caret in some other part of the page means no correction should even be considered.
    function collapsedCaretIn(editor) {
        const selection = window.getSelection();
        if (selection.rangeCount === 0) return null;
        if (!selection.isCollapsed) return null;
        const caret = selection.getRangeAt(0);
        if (!editor.contains(caret.startContainer)) return null;
        return caret;
    }

    // The text of the current line up to the caret (tail-capped). Null means the caret could
    // not be read; "" means the caret sits at the start of a line.
    function textBeforeCaret(editor) {
        const caret = collapsedCaretIn(editor);
        if (caret === null) return null;
        let node = caret.startContainer;
        let text = "";
        if (node.nodeType === Node.TEXT_NODE) {
            text = node.textContent.slice(0, caret.startOffset);
        } else if (caret.startOffset > 0) {
            node = lastLeafInside(node.childNodes[caret.startOffset - 1]);
            if (isLineBreak(node)) return text;
            if (node.nodeType === Node.TEXT_NODE) text = node.textContent;
        } else if (isLineBreak(node)) {
            // The caret sits at the start of an empty block (a fresh Enter): the line starts here.
            return text;
        }
        while (text.length < ENOUGH_CONTEXT) {
            const leaf = previousLeaf(node, editor);
            if (leaf === null) break;
            if (isLineBreak(leaf)) break;
            if (leaf.nodeType === Node.TEXT_NODE) text = leaf.textContent + text;
            node = leaf;
        }
        return text;
    }

    // Replace [fromOffset, toOffset) of a text node via execCommand so the edit joins the
    // editor's undo stack and lands the caret after the replacement, like ordinary typing.
    function replaceTextRange(node, fromOffset, toOffset, replacement) {
        const selection = window.getSelection();
        selection.collapse(node, toOffset);
        selection.extend(node, fromOffset);
        document.execCommand("insertText", false, replacement);
    }

    return { collapsedCaretIn, textBeforeCaret, replaceTextRange };
})();
