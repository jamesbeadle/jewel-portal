// The 3D model on a lead's page (Sales → Leads → lead → 3D model, 2026-09-15): the house the
// enquiry is about, built in the browser from the architect's drawings, with the works played
// as a build-up — scaffold, roof opened, dormer raised and glazed, rooflights, the garage turned
// into a room — and the camera handed over at the end to look around. The scene is the ES
// module tree under js/house-model/ on a vendored three.js (js/vendor/three/, named by the
// import map in index.html) — both fetched the first time a model is mounted, so nobody pays
// for them on pages that never show one. This file is the classic-script doorway Blazor calls.
window.jpmsHouseModel = (function () {
    let viewerLoading = null;

    function viewer() {
        if (!viewerLoading) viewerLoading = import(new URL("js/house-model/viewer.js", document.baseURI).href);
        return viewerLoading;
    }

    const call = name => async function (host, ...rest) {
        const module = await viewer();
        return module[name](host, ...rest);
    };

    return {
        // Build the named model into `host` and draw it. Rejects (so the panel can say so) when
        // the scene code can't be fetched or the browser has no WebGL.
        mount: call("mount"),
        play: call("play"),
        pause: call("pause"),
        skipToEnd: call("skipToEnd"),
        setProposed: call("setProposed"),
        view: call("view"),
        toggleFullscreen: call("toggleFullscreen"),
        dispose: call("dispose")
    };
})();
