// The moves a part can make during the build-up. Each animator answers two calls: reset() puts
// its part back to how it stood before the works began, apply(progress) moves it to where it
// stands that far through its step — and at progress 1 leaves it exactly as the finished house
// needs it, so switching Existing / Proposed afterwards finds nothing out of place.
import * as THREE from "three";

const MinimumScale = 0.001;
const easeInOut = t => (t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2);

// Rises out of its anchor: scale up from nothing.
export function grow(part) {
    return {
        reset: () => { part.visible = false; part.scale.y = MinimumScale; },
        apply: progress => { part.visible = true; part.scale.y = Math.max(MinimumScale, easeInOut(progress)); }
    };
}

// The reverse — a scaffold coming down.
export function shrink(part) {
    return {
        reset: () => {},
        apply: progress => { part.scale.y = Math.max(MinimumScale, 1 - easeInOut(progress)); part.visible = progress < 1; }
    };
}

// Slides into its place from an offset in metres.
export function drop(part, step) {
    const rest = part.position.clone();
    const offset = new THREE.Vector3(...step.from);
    return {
        reset: () => { part.visible = false; part.position.copy(rest); },
        apply: progress => { part.visible = true; part.position.copy(rest).addScaledVector(offset, 1 - easeInOut(progress)); }
    };
}

// Carried away by an offset and gone — back in its place, unseen, once it has left.
export function lift(part, step) {
    const rest = part.position.clone();
    const offset = new THREE.Vector3(...step.by);
    return {
        reset: () => { part.visible = true; part.position.copy(rest); },
        apply: progress => {
            part.visible = progress < 1;
            part.position.copy(rest).addScaledVector(offset, progress < 1 ? easeInOut(progress) : 0);
        }
    };
}

export function appear(part) {
    return { reset: () => { part.visible = false; }, apply: () => { part.visible = true; } };
}

export function vanish(part) {
    return { reset: () => {}, apply: progress => { if (progress >= 1) part.visible = false; } };
}

// Every see-through surface under the part comes up from clear to its own opacity. The panes
// get their own copy of the material so nothing else in the house fades with them.
export function fade(part, step, materials) {
    const panes = [];
    part.traverse(mesh => {
        if (!mesh.isMesh || !mesh.material.transparent) return;
        mesh.material = mesh.material.clone();
        materials.owned.push(mesh.material);
        panes.push({ mesh, opacity: mesh.material.opacity });
    });
    return {
        reset: () => {
            part.visible = false;
            panes.forEach(({ mesh }) => { mesh.visible = false; mesh.material.opacity = 0; });
        },
        apply: progress => {
            part.visible = true;
            panes.forEach(({ mesh, opacity }) => { mesh.visible = true; mesh.material.opacity = opacity * easeInOut(progress); });
        }
    };
}
