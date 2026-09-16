// The frame loop: the playback advances, the camera orbits slowly while it plays, the controls
// settle, and a frame is drawn only when something moved.
import * as THREE from "three";

const Up = new THREE.Vector3(0, 1, 0);
const OrbitRadiansPerSecond = 0.06;

export function orbit(state, now) {
    const seconds = state.lastOrbitAt === null ? 0 : (now - state.lastOrbitAt) / 1000;
    state.lastOrbitAt = now;
    const { camera, controls } = state.stage;
    const offset = camera.position.clone().sub(controls.target).applyAxisAngle(Up, OrbitRadiansPerSecond * seconds);
    camera.position.copy(controls.target).add(offset);
}

export function startFrames(state) {
    state.lastOrbitAt = null;
    const frame = now => {
        if (state.disposed) return;
        const playing = state.playback.step(now);
        if (playing) orbit(state, now); else state.lastOrbitAt = null;
        if (!playing && state.isRecordingUntilFinished) state.finishRecording();
        const orbited = state.stage.controls.update();
        if (orbited || playing || state.needsFrame) { state.stage.render(); state.needsFrame = false; }
        state.frameRequest = requestAnimationFrame(frame);
    };
    state.frameRequest = requestAnimationFrame(frame);
}

export function followSize(state) {
    state.resizeObserver = new ResizeObserver(() => { if (!state.disposed) { state.stage.resize(); state.needsFrame = true; } });
    state.resizeObserver.observe(state.host);
}

export function stopFrames(state) {
    cancelAnimationFrame(state.frameRequest);
    state.resizeObserver.disconnect();
}
