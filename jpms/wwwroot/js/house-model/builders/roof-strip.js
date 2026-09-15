// The patch of roof stripped back to felt and battens where the dormer is to go: a flat panel
// lying just proud of the tiles over the dormer's footprint, drawn with a batten texture. It is
// built fully opaque; the build-up's fade takes it from clear to that before the dormer rises.
import * as THREE from "three";
import { slopeFrame, orientToSlope } from "./gable-roof.js";
import { Palette } from "../palette.js";

const ProudOfTiles = 0.015;
const CanvasSize = 256;
const BattenRows = 6;

function battenTexture() {
    const canvas = document.createElement("canvas");
    canvas.width = CanvasSize;
    canvas.height = CanvasSize;
    const context = canvas.getContext("2d");
    context.fillStyle = Palette.roofFelt;
    context.fillRect(0, 0, CanvasSize, CanvasSize);
    context.fillStyle = Palette.batten;
    const rowHeight = CanvasSize / BattenRows;
    for (let row = 0; row < BattenRows; row++) context.fillRect(0, row * rowHeight, CanvasSize, rowHeight * 0.18);
    const texture = new THREE.CanvasTexture(canvas);
    texture.wrapS = THREE.RepeatWrapping;
    texture.wrapT = THREE.RepeatWrapping;
    texture.colorSpace = THREE.SRGBColorSpace;
    return texture;
}

export function buildRoofStrip(strip, block, materials) {
    const frame = slopeFrame(block, strip.side);
    const width = strip.x[1] - strip.x[0];
    const [nearEdge, farEdge] = strip.alongSlope;
    const length = farEdge - nearEdge;
    const material = new THREE.MeshStandardMaterial({ map: battenTexture(), roughness: 1, transparent: true, opacity: 1 });
    material.map.repeat.set(width, length / 2);
    const patch = new THREE.Mesh(new THREE.PlaneGeometry(width, length), material);
    patch.rotation.x = -Math.PI / 2;
    const placed = new THREE.Group();
    placed.name = strip.name;
    placed.add(patch);
    orientToSlope(placed, frame);
    placed.position.copy(frame.origin)
        .addScaledVector(frame.up, nearEdge + length / 2)
        .addScaledVector(frame.normal, ProudOfTiles)
        .setX((strip.x[0] + strip.x[1]) / 2);
    materials.owned.push(material);
    return placed;
}
