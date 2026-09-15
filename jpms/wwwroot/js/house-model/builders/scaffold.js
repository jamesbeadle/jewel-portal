// The scaffold that goes up for the roof works: two rows of standards along a wall, and at
// each lift the ledgers between them, the boards across them and a guard rail outside. The
// standards are one group rooted at the ground so they can grow up out of it; each lift is its
// own group so the build-up can land them one at a time. Construction-phase only.
import * as THREE from "three";
import { boxMesh } from "./box-in-metres.js";
import { GroundLevel } from "../ground-level.js";
import { stampPhase } from "../phases.js";

const TubeRadius = 0.024;
const LiftHeight = 2.0;
const GuardRailHeights = [0.5, 1.0];
const BoardThickness = 0.04;

function tube(length, material) {
    const geometry = new THREE.CylinderGeometry(TubeRadius, TubeRadius, length, 8);
    geometry.translate(0, length / 2, 0);
    return new THREE.Mesh(geometry, material);
}

function horizontalTube(from, to, material) {
    const run = to.clone().sub(from);
    const piece = tube(run.length(), material);
    piece.position.copy(from);
    piece.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), run.normalize());
    return piece;
}

function standards(scaffold, materials) {
    const group = new THREE.Group();
    group.name = "scaffold standards";
    group.position.y = GroundLevel;
    const height = scaffold.lifts * LiftHeight + GuardRailHeights[1];
    for (const x of scaffold.standardsAt) {
        for (const z of scaffold.rowsAt) {
            const pole = tube(height, materials.scaffoldTube);
            pole.position.set(x, 0, z);
            group.add(pole);
        }
    }
    return group;
}

function lift(scaffold, number, materials) {
    const group = new THREE.Group();
    group.name = `scaffold lift ${number}`;
    const height = GroundLevel + number * LiftHeight;
    const [inner, outer] = scaffold.rowsAt;
    const [first, last] = [scaffold.standardsAt[0], scaffold.standardsAt[scaffold.standardsAt.length - 1]];
    for (const z of scaffold.rowsAt) {
        group.add(horizontalTube(new THREE.Vector3(first, height, z), new THREE.Vector3(last, height, z), materials.scaffoldTube));
    }
    for (const x of scaffold.standardsAt) {
        group.add(horizontalTube(new THREE.Vector3(x, height, inner), new THREE.Vector3(x, height, outer), materials.scaffoldTube));
    }
    for (const rail of GuardRailHeights) {
        group.add(horizontalTube(new THREE.Vector3(first, height + rail, outer), new THREE.Vector3(last, height + rail, outer), materials.scaffoldTube));
    }
    const boards = boxMesh(last - first, BoardThickness, outer - inner, materials.scaffoldBoards);
    boards.position.set((first + last) / 2, height + TubeRadius + BoardThickness / 2, (inner + outer) / 2);
    group.add(boards);
    return group;
}

// The standards and each lift are the parts the build-up moves, so each carries the phase
// itself — a phase on the whole scaffold would hide them under the build-up's feet.
export function buildScaffold(scaffold, materials) {
    const group = new THREE.Group();
    group.name = "scaffold";
    group.add(stampPhase(standards(scaffold, materials), scaffold.phase));
    for (let number = 1; number <= scaffold.lifts; number++) {
        group.add(stampPhase(lift(scaffold, number, materials), scaffold.phase));
    }
    return group;
}
