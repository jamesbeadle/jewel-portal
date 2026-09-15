// A pitched roof with its ridge along x: two tiled slopes with an overhang at the eaves, fascia,
// gutter and soffit along each, a bargeboard down each open verge, and a ridge along the top.
// `slopeFrame` is the same geometry handed to whatever else sits on a slope — rooflights, the
// dormer — so everything on the roof agrees about where the roof is.
import * as THREE from "three";
import { boxMesh } from "./box-in-metres.js";

export const SlabThickness = 0.12;
const FasciaHeight = 0.2;
const BargeboardHeight = 0.22;
const RidgeRadius = 0.085;
const AlongX = new THREE.Vector3(1, 0, 0);

export function slopeFrame(block, side) {
    const halfSpan = block.depth / 2;
    const rise = block.roof.ridge - block.eaves;
    const pitch = Math.atan2(rise, halfSpan);
    const towardsRidge = side === "front" ? 1 : -1;
    return {
        origin: new THREE.Vector3(block.x, block.eaves, side === "front" ? block.z : block.z + block.depth),
        along: new THREE.Vector3(towardsRidge, 0, 0),
        up: new THREE.Vector3(0, Math.sin(pitch), towardsRidge * Math.cos(pitch)),
        normal: new THREE.Vector3(0, Math.cos(pitch), -towardsRidge * Math.sin(pitch)),
        outwardZ: -towardsRidge,
        pitch,
        length: Math.hypot(rise, halfSpan)
    };
}

// The slope's own axes — along the eaves, out of the tiles, up to the ridge — as a right-handed
// set: `along` flips with the side so the triple stays a rotation and never a reflection.
export function orientToSlope(object, frame) {
    object.quaternion.setFromRotationMatrix(new THREE.Matrix4().makeBasis(frame.along, frame.normal, frame.up));
}

function slope(block, side, materials) {
    const frame = slopeFrame(block, side);
    const { overhang, vergeLeft, vergeRight } = block.roof;
    const slabWidth = block.width + vergeLeft + vergeRight;
    const xCentre = block.x - vergeLeft + slabWidth / 2;
    const slabLength = frame.length + overhang;
    const slab = boxMesh(slabWidth, SlabThickness, slabLength, materials.tiles);
    orientToSlope(slab, frame);
    slab.position.copy(frame.origin)
        .addScaledVector(frame.up, (frame.length - overhang) / 2)
        .addScaledVector(frame.normal, -SlabThickness / 2)
        .setX(xCentre);
    const parts = [slab, ...eavesLine(frame, block, xCentre, slabWidth, materials)];
    if (vergeRight > 0) parts.push(bargeboard(slab, slabWidth, slabLength, materials));
    return parts;
}

function eavesLine(frame, block, xCentre, slabWidth, materials) {
    const edge = frame.origin.clone().addScaledVector(frame.up, -block.roof.overhang);
    const fascia = boxMesh(slabWidth, FasciaHeight, 0.03, materials.fascia);
    fascia.position.set(xCentre, edge.y - FasciaHeight / 2 - 0.02, edge.z);
    const gutter = boxMesh(slabWidth, 0.1, 0.11, materials.gutter);
    gutter.position.set(xCentre, edge.y - 0.24, edge.z + frame.outwardZ * 0.07);
    const soffitDepth = Math.abs(edge.z - frame.origin.z);
    const soffit = boxMesh(slabWidth, 0.02, soffitDepth, materials.fascia);
    soffit.position.set(xCentre, edge.y - FasciaHeight - 0.03, (edge.z + frame.origin.z) / 2);
    return [fascia, gutter, soffit];
}

function bargeboard(slab, slabWidth, slabLength, materials) {
    const board = boxMesh(0.03, BargeboardHeight, slabLength, materials.fascia);
    board.quaternion.copy(slab.quaternion);
    board.position.copy(slab.position)
        .addScaledVector(AlongX, slabWidth / 2 - 0.015)
        .addScaledVector(new THREE.Vector3(0, 1, 0).applyQuaternion(slab.quaternion), -0.1);
    return board;
}

function ridge(block, materials) {
    const { vergeLeft, vergeRight } = block.roof;
    const length = block.width + vergeLeft + vergeRight;
    const cap = new THREE.Mesh(new THREE.CylinderGeometry(RidgeRadius, RidgeRadius, length, 12), materials.ridge);
    cap.rotation.z = Math.PI / 2;
    cap.position.set(block.x - vergeLeft + length / 2, block.roof.ridge - 0.02, block.z + block.depth / 2);
    return cap;
}

export function gableRoof(block, materials) {
    const roof = new THREE.Group();
    roof.name = `${block.name} roof`;
    roof.add(...slope(block, "front", materials), ...slope(block, "rear", materials), ridge(block, materials));
    return roof;
}
