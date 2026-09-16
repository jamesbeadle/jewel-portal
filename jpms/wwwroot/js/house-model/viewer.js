// One mounted model per host element: the stage, the house with its temporary and internal
// works, the context, the camera, and the build-up's timeline, kept in a map keyed by the host
// (as image-viewer.js does) so Blazor can address a model by the element it rendered. The model
// opens on the house as it stands, drifting slowly, and waits for Play; the camera is the
// viewer's own only once the works are finished. A definition is the estimate's own plain data.
import { createStage } from "./stage.js";
import { createMaterials, disposeMaterials } from "./materials.js";
import { buildHouse, buildTemporaryWorks } from "./house-builder.js";
import { buildContext } from "./builders/context.js";
import { buildElements } from "./builders/elements.js";
import { applyPhase } from "./phases.js";
import { Modes, applyMode } from "./modes.js";
import { houseViews, ViewNames } from "./camera-views.js";
import { CameraFlight } from "./camera-flight.js";
import { compileProgramme } from "./build-sequence/programme.js";
import { Timeline } from "./build-sequence/timeline.js";
import { timelineHooks } from "./build-sequence/hooks.js";
import { startFrames, stopFrames, followSize, reportFullscreen } from "./frames.js";

const states = new Map();

export function mount(host, definition, dotnetRef) {
    if (!host || states.has(host)) return false;
    if (!definition?.blocks?.length) throw new Error("the model definition has no blocks");
    const materials = createMaterials();
    const stage = createStage(host);
    const house = buildHouse(definition, materials);
    house.add(buildTemporaryWorks(definition, materials));
    const elements = buildElements(definition, materials);
    elements.visible = false;
    house.add(elements);
    stage.scene.add(house, buildContext(definition.context, materials));
    const state = { host, dotnetRef, stage, materials, house, elements, views: houseViews(definition), mode: Modes.existing,
        flight: new CameraFlight(stage.camera, stage.controls), needsFrame: true, isDrifting: true, disposed: false };
    states.set(host, state);
    applyPhase(house, false);
    state.timeline = new Timeline(compileProgramme(definition.programme, house, materials), timelineHooks(state));
    stage.controls.enabled = false;
    state.flight.jumpTo(state.views[ViewNames.overview]);
    followSize(state);
    reportFullscreen(state);
    startFrames(state);
    dotnetRef.invokeMethodAsync("ModelReady").catch(() => {});
    return true;
}

export function play(host) {
    const state = states.get(host);
    if (!state) return;
    if (state.mode === Modes.works) setMode(host, Modes.proposed);
    state.timeline.play();
}

export function pause(host) {
    states.get(host)?.timeline.pause();
}

export function skipToEnd(host) {
    states.get(host)?.timeline.skipToEnd();
}

export function setMode(host, mode) {
    const state = states.get(host);
    if (state) applyMode(state, mode);
}

export function view(host, viewName) {
    const state = states.get(host);
    const destination = state?.views[viewName];
    if (destination) state.flight.flyTo(destination);
}

export function toggleFullscreen(host) {
    if (!states.has(host)) return;
    if (document.fullscreenElement === host) document.exitFullscreen();
    else host.requestFullscreen?.();
}

export function dispose(host) {
    const state = states.get(host);
    if (!state) return;
    state.disposed = true;
    stopFrames(state);
    state.house.traverse(part => part.geometry?.dispose());
    disposeMaterials(state.materials);
    state.stage.dispose();
    states.delete(host);
}
