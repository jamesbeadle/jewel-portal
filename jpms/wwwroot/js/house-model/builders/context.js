// The ground the house stands on and what stands around it: lawn, the paved drive and patio,
// the garden fences, and the neighbours as quiet grey massing — there for scale and for the
// party wall, never the subject.
import * as THREE from "three";
import { boxMesh } from "./box-in-metres.js";
import { wallBlock, gablePrism } from "./wall-blocks.js";
import { gableRoof } from "./gable-roof.js";
import { GroundLevel } from "../ground-level.js";
import { castsAndReceivesShadows } from "../shadows.js";

const GroundExtent = 140;
const PavingThickness = 0.02;
const FenceHeight = 1.8;
const FenceThickness = 0.05;

function lawn(materials) {
    const ground = new THREE.Mesh(new THREE.PlaneGeometry(GroundExtent, GroundExtent), materials.lawn);
    ground.rotation.x = -Math.PI / 2;
    ground.position.y = GroundLevel;
    ground.receiveShadow = true;
    ground.name = "lawn";
    return ground;
}

function paving(area, materials) {
    const width = area.x[1] - area.x[0];
    const depth = area.z[1] - area.z[0];
    const slab = boxMesh(width, PavingThickness, depth, materials.paving);
    slab.position.set(area.x[0] + width / 2, GroundLevel + PavingThickness / 2, area.z[0] + depth / 2);
    slab.receiveShadow = true;
    return slab;
}

function fence(run, materials) {
    const [fromX, fromZ] = run.from;
    const [toX, toZ] = run.to;
    const length = Math.hypot(toX - fromX, toZ - fromZ);
    const panel = new THREE.Group();
    const boards = boxMesh(length, FenceHeight, FenceThickness, materials.fence);
    boards.position.y = FenceHeight / 2;
    const rail = boxMesh(length, 0.05, 0.09, materials.fence);
    rail.position.y = FenceHeight;
    panel.add(boards, rail);
    panel.position.set((fromX + toX) / 2, GroundLevel, (fromZ + toZ) / 2);
    panel.rotation.y = -Math.atan2(toZ - fromZ, toX - fromX);
    return panel;
}

function neighbour(block, materials) {
    const quiet = materials.neighbour;
    const quietMaterials = { brick: quiet, tiles: quiet, fascia: quiet, gutter: quiet, ridge: quiet };
    const house = new THREE.Group();
    house.name = `neighbour ${block.name}`;
    house.add(wallBlock(block, quietMaterials), gablePrism(block, quietMaterials), gableRoof(block, quietMaterials));
    return house;
}

export function buildContext(context, materials) {
    const group = new THREE.Group();
    group.name = "context";
    group.add(lawn(materials));
    context.paving.forEach(area => group.add(paving(area, materials)));
    context.fences.forEach(run => group.add(fence(run, materials)));
    context.neighbours.forEach(block => group.add(neighbour(block, materials)));
    castsAndReceivesShadows(group);
    group.getObjectByName("lawn").castShadow = false;
    return group;
}
