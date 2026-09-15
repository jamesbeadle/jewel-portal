// New brickwork filling an old opening — the garage door bricked up. A panel a little proud of
// the wall so it reads as new work, rooted at its base so the build-up can lay it up from the
// ground.
import * as THREE from "three";
import { boxInMetres } from "./box-in-metres.js";
import { placeOnFace } from "./faces.js";

const ProudOfWall = 0.02;
const Thickness = 0.1;

export function buildBrickInfill(infill, faces, materials) {
    const width = infill.u[1] - infill.u[0];
    const height = infill.y[1] - infill.y[0];
    const geometry = boxInMetres(width, height, Thickness);
    geometry.translate(0, height / 2, ProudOfWall - Thickness / 2);
    const panel = new THREE.Mesh(geometry, materials.brick);
    panel.name = infill.name;
    return placeOnFace(panel, faces[infill.face], (infill.u[0] + infill.u[1]) / 2, infill.y[0]);
}
