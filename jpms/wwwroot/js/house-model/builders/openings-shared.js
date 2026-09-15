// What every opening has in common: its size and centre read off the definition's edges, and
// the dark reveal drawn on the wall behind it so the frame reads as set into brickwork.
import * as THREE from "three";

export const RevealOffset = 0.008;

export function sizeOf(opening) {
    return {
        width: opening.u[1] - opening.u[0],
        height: opening.y[1] - opening.y[0],
        along: (opening.u[0] + opening.u[1]) / 2,
        centreHeight: (opening.y[0] + opening.y[1]) / 2
    };
}

export function reveal(width, height, materials) {
    const shadow = new THREE.Mesh(new THREE.PlaneGeometry(width + 0.02, height + 0.02), materials.reveal);
    shadow.position.z = RevealOffset;
    return shadow;
}
