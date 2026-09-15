// The solid brickwork of a block: its walls up to the eaves, and the triangular gable that
// carries the roof between them. Both are simple solids — nothing is hollowed out, because
// nothing inside is ever seen — with brick coursing in metres on every face.
import * as THREE from "three";
import { boxMesh } from "./box-in-metres.js";
import { SlabThickness, slopeFrame } from "./gable-roof.js";
import { GroundLevel } from "../ground-level.js";

// The walls stop a little short of the eaves line so their top edge sits under the roof slab
// rather than grazing its surface; the fascia and soffit close the eaves from outside.
const EavesOverlap = 0.1;

export function wallBlock(block, materials) {
    const height = block.eaves - EavesOverlap - GroundLevel;
    const walls = boxMesh(block.width, height, block.depth, materials.brick);
    walls.position.set(block.x + block.width / 2, GroundLevel + height / 2, block.z + block.depth / 2);
    walls.name = `${block.name} walls`;
    return walls;
}

// The gable prism spans the whole block, ridge along x: a triangle in the z–y plane extruded
// across the width. Extruding along the shape's own z and turning the result puts the
// extrusion along world x, with the brick coursing horizontal on the visible ends. The
// triangle is dropped by the slab's thickness so its slopes meet the slab's underside and
// never fight the tiles for the same surface.
export function gablePrism(block, materials) {
    const drop = SlabThickness / Math.cos(slopeFrame(block, "front").pitch);
    const shape = new THREE.Shape();
    shape.moveTo(block.z, block.eaves - drop);
    shape.lineTo(block.z + block.depth / 2, block.roof.ridge - drop);
    shape.lineTo(block.z + block.depth, block.eaves - drop);
    shape.closePath();
    const geometry = new THREE.ExtrudeGeometry(shape, { depth: block.width, bevelEnabled: false });
    geometry.rotateY(-Math.PI / 2);
    geometry.translate(block.x + block.width, 0, 0);
    const gable = new THREE.Mesh(geometry, materials.brick);
    gable.name = `${block.name} gable`;
    return gable;
}
