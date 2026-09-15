// The downpipes, one at each corner the drawings mark RWP: a dark pipe from the gutter to the
// ground, tight to the wall.
import * as THREE from "three";
import { GroundLevel } from "../ground-level.js";

const PipeRadius = 0.035;

export function buildDownpipe(downpipe, materials) {
    const height = downpipe.top - GroundLevel;
    const pipe = new THREE.Mesh(new THREE.CylinderGeometry(PipeRadius, PipeRadius, height, 10), materials.gutter);
    pipe.position.set(downpipe.x, GroundLevel + height / 2, downpipe.z);
    pipe.name = "downpipe";
    return pipe;
}
