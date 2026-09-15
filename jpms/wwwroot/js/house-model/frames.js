// The frame loop and what rides on it. Every frame the build-up advances, a camera flight moves
// on, and — while the viewer is watching rather than steering — the camera drifts slowly round
// its target so nothing stands still. A frame is drawn only when something moved.
import * as THREE from "three";

const Up = new THREE.Vector3(0, 1, 0);
const DriftRadiansPerSecond = 0.045;

function driftCamera(state, now) {
    const seconds = state.lastDriftAt === null ? 0 : (now - state.lastDriftAt) / 1000;
    state.lastDriftAt = now;
    const { camera, controls } = state.stage;
    const offset = camera.position.clone().sub(controls.target).applyAxisAngle(Up, DriftRadiansPerSecond * seconds);
    camera.position.copy(controls.target).add(offset);
}

export function startFrames(state) {
    state.lastDriftAt = null;
    const frame = now => {
        if (state.disposed) return;
        const building = state.timeline.step(now);
        const flying = state.flight.step();
        if (state.isDrifting && !flying) driftCamera(state, now);
        if (!state.isDrifting) state.lastDriftAt = null;
        const orbited = state.stage.controls.update();
        if (orbited || flying || building || state.isDrifting || state.needsFrame) {
            state.stage.render();
            state.needsFrame = false;
        }
        state.frameRequest = requestAnimationFrame(frame);
    };
    state.frameRequest = requestAnimationFrame(frame);
}

export function followSize(state) {
    state.resizeObserver = new ResizeObserver(() => {
        if (state.disposed) return;
        state.stage.resize();
        state.needsFrame = true;
    });
    state.resizeObserver.observe(state.host);
}

export function reportFullscreen(state) {
    state.onFullscreenChange = () => {
        state.needsFrame = true;
        state.dotnetRef.invokeMethodAsync("FullscreenChanged", document.fullscreenElement === state.host).catch(() => {});
    };
    document.addEventListener("fullscreenchange", state.onFullscreenChange);
}

export function stopFrames(state) {
    cancelAnimationFrame(state.frameRequest);
    state.resizeObserver.disconnect();
    document.removeEventListener("fullscreenchange", state.onFullscreenChange);
}
