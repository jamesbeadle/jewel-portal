// The openings in the walls — windows, the patio doors, and the doors from doors.js — each
// built flat facing +z around its own centre, then placed on its face. A window is a dark
// reveal, the white frame and glass, a cill, and the brick arch over its head that every
// window on this house has.
import * as THREE from "three";
import { glazingUnit } from "./glazing-unit.js";
import { placeOnFace } from "./faces.js";
import { frontDoor, garageDoor } from "./doors.js";
import { reveal, sizeOf, RevealOffset } from "./openings-shared.js";

const ArchRise = 0.11;
const ArchHeight = 0.16;

function cill(width, height, materials) {
    const stone = new THREE.Mesh(new THREE.BoxGeometry(width + 0.12, 0.06, 0.12), materials.frameWhite);
    stone.position.set(0, -height / 2 - 0.03, 0.04);
    return stone;
}

// A segmental arch: the ring through the window's top corners, rising ArchRise at the crown.
function brickArch(width, height, materials) {
    const chord = width + 0.1;
    const radius = (chord * chord / 4 + ArchRise * ArchRise) / (2 * ArchRise);
    const halfAngle = Math.asin(chord / 2 / radius);
    const ring = new THREE.RingGeometry(radius, radius + ArchHeight, 32, 1, Math.PI / 2 - halfAngle, 2 * halfAngle);
    const arch = new THREE.Mesh(ring, materials.brickArch);
    arch.position.set(0, height / 2 - radius + ArchRise, RevealOffset + 0.004);
    return arch;
}

function windowOpening(opening, materials) {
    const { width, height } = sizeOf(opening);
    const group = new THREE.Group();
    group.add(reveal(width, height, materials), cill(width, height, materials),
        glazingUnit({ width, height, columns: opening.columns }, materials.frameWhite, materials.glass));
    if (opening.arched) group.add(brickArch(width, height, materials));
    return group;
}

function slidingDoors(opening, materials) {
    const { width, height } = sizeOf(opening);
    const group = new THREE.Group();
    group.add(reveal(width, height, materials),
        glazingUnit({ width, height, columns: opening.columns, frameWidth: 0.08 }, materials.frameWhite, materials.glass));
    return group;
}

const buildersByKind = { window: windowOpening, slidingDoors, door: frontDoor, garageDoor };

export function buildOpening(opening, faces, materials) {
    const built = buildersByKind[opening.kind](opening, materials);
    const { along, centreHeight } = sizeOf(opening);
    built.name = opening.name ?? `${opening.kind} on ${opening.face}`;
    return placeOnFace(built, faces[opening.face], along, centreHeight);
}
