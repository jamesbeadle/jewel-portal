// Keeps the assistant transcript on its newest line. The transcript is a plain overflow column
// with no scroll behaviour of its own, so once a chat outgrew the panel a sent message, the
// working indicator and the reply all landed below the fold — the user pressed Send and saw
// nothing move. The panel asks for a scroll after every render that added to the transcript.
// The user's own actions (send, retry, carry on, opening a past chat) always land at the
// bottom; a hop arriving while they have scrolled up to re-read something leaves them there,
// and the next thing they send pins the view again.
window.jpmsChatScroll = (() => {
    const nearBottomPixels = 48;

    const isNearBottom = transcript =>
        transcript.scrollHeight - transcript.scrollTop - transcript.clientHeight <= nearBottomPixels;

    // Whether the view was at the bottom is remembered from the LAST scroll event: by the time
    // the panel asks, the new content is already in the DOM and the geometry says "not at the
    // bottom" for everyone. Appended content fires no scroll event, so the answer stays true
    // until the user actually moves the view.
    const watch = transcript => {
        if (transcript.dataset.jpmsScrollWatched === "1") return;
        transcript.dataset.jpmsScrollWatched = "1";
        transcript.dataset.jpmsPinned = "1";
        transcript.addEventListener("scroll", () => {
            transcript.dataset.jpmsPinned = isNearBottom(transcript) ? "1" : "0";
        }, { passive: true });
    };

    return {
        toBottom: (transcriptId, isUserAction) => {
            const transcript = document.getElementById(transcriptId);
            if (!transcript) return;
            watch(transcript);
            if (!isUserAction && transcript.dataset.jpmsPinned !== "1") return;
            transcript.scrollTop = transcript.scrollHeight;
        }
    };
})();
