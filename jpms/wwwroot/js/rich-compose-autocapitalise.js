// Auto-capitalisation for the compose editor (rich-compose.js wires these handlers up).
// Deterministic, typing-only corrections — pasted and programmatic text is never touched:
//   • the first letter typed at the start of a sentence is capitalised — at the start of a
//     line, or after . ! ? and a space, unless the line ends in a known abbreviation;
//   • a standalone lowercase "i" becomes "I" once the next character proves it stood alone;
//   • Backspace immediately after a correction puts the lowercase letter back, and retyping
//     at that same spot is left alone, so the editor never fights a deliberate choice.
window.jpmsAutoCapitalise = (function () {
    const caretReader = window.jpmsComposeCaret;
    const SENTENCE_END = /[.!?]["')\]]?\s+$/;
    const ABBREVIATION_END = /(^|\s)(e\.g\.|i\.e\.|etc\.|no\.|approx\.|ref\.|max\.|min\.|inc\.)\s+$/i;
    const STANDALONE_I = /(^|\s)i$/;
    const PRONOUN_BOUNDARY = /^[\s.,!?;:'")\]]$/;
    const LOWERCASE_LETTER = /^[a-z]$/;
    const lastCorrectionByEditor = new WeakMap();
    const declinedSpotByEditor = new WeakMap();
    let isApplyingCorrection = false;

    function isSentenceStart(lineText) {
        if (lineText.trim() === "") return true;
        if (!SENTENCE_END.test(lineText)) return false;
        return !ABBREVIATION_END.test(lineText);
    }

    function userDeclinedHere(editor, caret) {
        const declined = declinedSpotByEditor.get(editor);
        if (!declined) return false;
        declinedSpotByEditor.delete(editor);
        if (caret === null) return false;
        return caret.startContainer === declined.node && caret.startOffset === declined.caretOffset;
    }

    function rememberCorrection(editor, kind, original, upper, charsBehindCaret) {
        const caret = caretReader.collapsedCaretIn(editor);
        if (caret === null) return;
        if (caret.startContainer.nodeType !== Node.TEXT_NODE) return;
        const upperIndex = caret.startOffset - charsBehindCaret;
        lastCorrectionByEditor.set(editor, { node: caret.startContainer, kind, original, upper, upperIndex });
    }

    function capitaliseSentenceStart(event, editor, typedLetter) {
        const lineText = caretReader.textBeforeCaret(editor);
        if (lineText === null) return;
        if (!isSentenceStart(lineText)) return;
        if (userDeclinedHere(editor, caretReader.collapsedCaretIn(editor))) return;
        event.preventDefault();
        const upper = typedLetter.toUpperCase();
        isApplyingCorrection = true;
        document.execCommand("insertText", false, upper);
        isApplyingCorrection = false;
        rememberCorrection(editor, "sentence", typedLetter, upper, 1);
    }

    function capitalisePronounI(event, editor, typedBoundary) {
        const lineText = caretReader.textBeforeCaret(editor);
        if (lineText === null) return;
        if (!STANDALONE_I.test(lineText)) return;
        const caret = caretReader.collapsedCaretIn(editor);
        if (caret.startContainer.nodeType !== Node.TEXT_NODE) return;
        if (caret.startOffset === 0) return;
        if (userDeclinedHere(editor, caret)) return;
        event.preventDefault();
        isApplyingCorrection = true;
        caretReader.replaceTextRange(caret.startContainer, caret.startOffset - 1, caret.startOffset, "I" + typedBoundary);
        isApplyingCorrection = false;
        rememberCorrection(editor, "pronoun", "i", "I", 2);
    }

    function onBeforeInput(event) {
        if (isApplyingCorrection) return;
        const editor = event.currentTarget;
        // Any fresh input except the Backspace we watch for means the last correction stood.
        if (event.inputType !== "deleteContentBackward") lastCorrectionByEditor.delete(editor);
        if (event.inputType !== "insertText") return;
        if (event.data === null || event.data.length !== 1) return;
        if (LOWERCASE_LETTER.test(event.data)) return capitaliseSentenceStart(event, editor, event.data);
        if (PRONOUN_BOUNDARY.test(event.data)) capitalisePronounI(event, editor, event.data);
    }

    function onKeyDown(event) {
        if (event.key !== "Backspace") return;
        const editor = event.currentTarget;
        const correction = lastCorrectionByEditor.get(editor);
        if (!correction) return;
        const caret = caretReader.collapsedCaretIn(editor);
        if (caret === null) return;
        if (caret.startContainer !== correction.node) return;
        if (caret.startOffset !== correction.upperIndex + 1) return;
        if (correction.node.textContent[correction.upperIndex] !== correction.upper) return;
        event.preventDefault();
        lastCorrectionByEditor.delete(editor);
        const declinedCaretOffset = correction.kind === "pronoun" ? correction.upperIndex + 1 : correction.upperIndex;
        declinedSpotByEditor.set(editor, { node: correction.node, caretOffset: declinedCaretOffset });
        isApplyingCorrection = true;
        caretReader.replaceTextRange(correction.node, correction.upperIndex, correction.upperIndex + 1, correction.original);
        isApplyingCorrection = false;
    }

    return { onBeforeInput, onKeyDown };
})();
