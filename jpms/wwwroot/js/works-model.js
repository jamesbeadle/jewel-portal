// The works view on a lead's page (Sales → Leads → lead → Works by trade, 2026-09-16, Jeremy's
// definition): the estimate's element list — every piece of the internal works as a footprint
// extruded between two heights, coloured by trade, joining the house in one phase and leaving
// in another — walked phase by phase with a scrubber, played with a slow orbit, filtered by
// trade, ghosted for what the works remove, inspected by a click, and recorded to a webm. The
// scene is the ES module tree under js/works-model/ on the vendored three.js, fetched the first
// time a model is mounted. This file is the classic-script doorway Blazor calls.
window.jpmsWorksModel = (function () {
    let viewerLoading = null;

    function viewer() {
        if (!viewerLoading) viewerLoading = import(new URL("js/works-model/viewer.js", document.baseURI).href);
        return viewerLoading;
    }

    const call = name => async function (host, ...rest) {
        const module = await viewer();
        return module[name](host, ...rest);
    };

    return {
        mount: async function (host, definitionJson, dotnetRef) {
            const module = await viewer();
            let definition;
            try { definition = JSON.parse(definitionJson); }
            catch { throw new Error("the stored model is not valid JSON"); }
            return module.mount(host, definition, dotnetRef);
        },
        play: call("play"),
        pause: call("pause"),
        setPhase: call("setPhase"),
        setTradeVisible: call("setTradeVisible"),
        setGhost: call("setGhost"),
        setShell: call("setShell"),
        startRecording: call("startRecording"),
        stopRecording: call("stopRecording"),
        renderAt: call("renderAt"),
        toggleFullscreen: call("toggleFullscreen"),
        dispose: call("dispose")
    };
})();
