// The doors: the front door with its handle under the canopy the elevation shows, and the
// up-and-over garage door the conversion bricks up. Built flat facing +z like every opening.
import * as THREE from "three";
import { glazingUnit } from "./glazing-unit.js";
import { reveal, sizeOf } from "./openings-shared.js";

const GarageDoorGrooves = 4;

function canopy(width, height, materials) {
    const hood = new THREE.Mesh(new THREE.BoxGeometry(width + 0.85, 0.12, 0.6), materials.fascia);
    hood.position.set(0, height / 2 + 0.55, 0.3);
    const brackets = [-1, 1].map(side => {
        const bracket = new THREE.Mesh(new THREE.BoxGeometry(0.06, 0.4, 0.45), materials.fascia);
        bracket.position.set(side * (width / 2 + 0.3), height / 2 + 0.29, 0.22);
        return bracket;
    });
    return [hood, ...brackets];
}

export function frontDoor(opening, materials) {
    const { width, height } = sizeOf(opening);
    const group = new THREE.Group();
    const leaf = new THREE.Mesh(new THREE.BoxGeometry(width - 0.12, height - 0.08, 0.03), materials.door);
    leaf.position.z = 0.015;
    const handle = new THREE.Mesh(new THREE.CylinderGeometry(0.012, 0.012, 0.14, 8), materials.frameDark);
    handle.position.set(width / 2 - 0.2, 0, 0.05);
    group.add(reveal(width, height, materials), leaf, handle,
        glazingUnit({ width, height, frameWidth: 0.06, frameDepth: 0.04 }, materials.frameWhite, materials.door));
    if (opening.canopy) group.add(...canopy(width, height, materials));
    return group;
}

export function garageDoor(opening, materials) {
    const { width, height } = sizeOf(opening);
    const group = new THREE.Group();
    const panel = new THREE.Mesh(new THREE.BoxGeometry(width, height, 0.05), materials.garageDoor);
    panel.position.z = 0.025;
    group.add(reveal(width, height, materials), panel);
    for (let groove = 1; groove <= GarageDoorGrooves; groove++) {
        const line = new THREE.Mesh(new THREE.BoxGeometry(width - 0.1, 0.02, 0.012), materials.gutter);
        line.position.set(0, -height / 2 + (height * groove) / (GarageDoorGrooves + 1), 0.05);
        group.add(line);
    }
    return group;
}
