// A click on an element opens its card: the pointer's ray against the visible elements, the
// nearest hit highlighted and told to the page. A drag is an orbit, not a click.
import * as THREE from "three";
import { elementMeshes } from "./elements.js";

const ClickToleranceSquared = 25;

export function listenForPicks(state) {
    const canvas = state.stage.canvas;
    const raycaster = new THREE.Raycaster();
    let pressedAt = null;
    state.onPointerDown = event => { pressedAt = { x: event.clientX, y: event.clientY }; };
    state.onPointerUp = event => {
        if (!pressedAt) return;
        const moved = (event.clientX - pressedAt.x) ** 2 + (event.clientY - pressedAt.y) ** 2;
        pressedAt = null;
        if (moved > ClickToleranceSquared) return;
        pick(state, raycaster, event);
    };
    canvas.addEventListener("pointerdown", state.onPointerDown);
    canvas.addEventListener("pointerup", state.onPointerUp);
}

function pick(state, raycaster, event) {
    const bounds = state.stage.canvas.getBoundingClientRect();
    const pointer = new THREE.Vector2(
        ((event.clientX - bounds.left) / bounds.width) * 2 - 1,
        -((event.clientY - bounds.top) / bounds.height) * 2 + 1);
    raycaster.setFromCamera(pointer, state.stage.camera);
    const hit = raycaster.intersectObjects(elementMeshes(state.works).filter(mesh => mesh.visible), false)[0];
    highlight(state, hit?.object ?? null);
    state.dotnetRef.invokeMethodAsync("ElementPicked", hit ? JSON.stringify(hit.object.userData.element) : null).catch(() => {});
}

export function highlight(state, mesh) {
    if (state.picked && state.picked.material.emissive) state.picked.material.emissive.setHex(0);
    state.picked = mesh;
    if (mesh) {
        mesh.material = mesh.material.clone();
        mesh.material.emissive = state.materials.highlightEmissive.clone();
        mesh.material.emissiveIntensity = 0.35;
    }
    state.needsFrame = true;
}

export function stopListeningForPicks(state) {
    state.stage.canvas.removeEventListener("pointerdown", state.onPointerDown);
    state.stage.canvas.removeEventListener("pointerup", state.onPointerUp);
}
