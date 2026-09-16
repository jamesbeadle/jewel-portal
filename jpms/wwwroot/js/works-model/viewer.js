// One mounted works model per host: the translucent shell and context, the elements by trade,
// the scrubber's phase, the playback and the recorder, kept in a map keyed by the host so
// Blazor addresses a model by the element it rendered. Orbit is the viewer's own from the
// start; play adds a slow orbit of its own.
import { createStage } from "../house-model/stage.js";
import * as THREE from "three";
import { createWorksMaterials, disposeWorksMaterials } from "./materials.js";
import { disposeMaterials } from "../house-model/materials.js";
import { createState } from "./state.js";
import { buildShell } from "./shell.js";
import { buildWorksElements } from "./elements.js";
import { present } from "./presentation.js";
import { Playback } from "./playback.js";
import { listenForPicks, stopListeningForPicks, highlight } from "./picking.js";
import { startRecording as beginRecording, stopRecording as endRecording } from "./recording.js";
import { startFrames, stopFrames, followSize, orbit } from "./frames.js";

const states = new Map();

// The opening view: high over the garden corner, looking at the middle of the house, so the
// whole plan reads and the orbit clears the roof.
const OpeningOffset = new THREE.Vector3(9, 11, 13);

function openOn(stage, definition) {
    const centre = new THREE.Vector3(...definition.centre);
    stage.camera.position.copy(centre).add(OpeningOffset);
    stage.controls.target.copy(centre);
    stage.controls.update();
}

export function mount(host, definition, dotnetRef) {
    if (!host || states.has(host)) return false;
    if (!definition?.phases?.length) throw new Error("the model has no phases to walk");
    const stage = createStage(host);
    stage.canvas = host.querySelector("canvas");
    const materials = createWorksMaterials(definition);
    const { shell, context, houseMaterials } = buildShell(definition, materials);
    const works = buildWorksElements(definition, materials);
    stage.scene.add(shell, context, works);
    const state = createState({ host, dotnetRef, definition, stage, materials, houseMaterials, shell, works });
    state.playback = new Playback(state);
    state.finishRecording = () => endRecording(state);
    states.set(host, state);
    openOn(stage, definition);
    present(state);
    listenForPicks(state);
    followSize(state);
    startFrames(state);
    dotnetRef.invokeMethodAsync("ModelReady", definition.phases.length).catch(() => {});
    return true;
}

export function play(host) { states.get(host)?.playback.play(); }

export function pause(host) { states.get(host)?.playback.pause(); }

export function setPhase(host, index) {
    change(host, state => { state.playback.pause(); state.phase = index; });
}

function change(host, apply) {
    const state = states.get(host);
    if (!state) return;
    apply(state);
    present(state);
}

export function setTradeVisible(host, trade, isVisible) {
    change(host, state => (isVisible ? state.hiddenTrades.delete(trade) : state.hiddenTrades.add(trade)));
}

export function setGhost(host, isGhosting) { change(host, state => { state.isGhosting = isGhosting; }); }

export function setShell(host, showsShell) { change(host, state => { state.showsShell = showsShell; }); }

export function startRecording(host) {
    const state = states.get(host);
    if (state) beginRecording(state, state.playback);
}

export function stopRecording(host) {
    const state = states.get(host);
    if (state) endRecording(state);
}

// For the headless renderer (tools/works-model/render-frames.mjs): a frame at a virtual time,
// so every frame lands exactly where the play would put it whatever the machine's speed. The
// first call takes the clock over from the frame loop.
export function renderAt(host, virtualMilliseconds) {
    const state = states.get(host);
    if (!state) return false;
    if (virtualMilliseconds === 0) { cancelAnimationFrame(state.frameRequest); state.phase = 0; state.playback.play(0); state.lastOrbitAt = null; }
    const playing = state.playback.step(virtualMilliseconds);
    if (playing) orbit(state, virtualMilliseconds);
    state.stage.controls.update();
    state.stage.render();
    return playing;
}

export function toggleFullscreen(host) {
    if (!states.has(host)) return;
    if (document.fullscreenElement === host) document.exitFullscreen(); else host.requestFullscreen?.();
}

export function dispose(host) {
    const state = states.get(host);
    if (!state) return;
    state.disposed = true;
    if (state.recorder) endRecording(state);
    stopFrames(state);
    stopListeningForPicks(state);
    highlight(state, null);
    state.stage.scene.traverse(part => part.geometry?.dispose());
    disposeWorksMaterials(state.materials);
    disposeMaterials(state.houseMaterials);
    state.stage.dispose();
    states.delete(host);
}
