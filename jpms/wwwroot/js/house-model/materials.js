// One set of materials per mounted model, built once and shared by every builder. Named for the
// building element they clothe, not for their colour, so a builder reads as construction.
import * as THREE from "three";
import { Palette } from "./palette.js";
import { brickTexture, tileTexture } from "./textures.js";

function matte(colour, roughness = 0.9) {
    return new THREE.MeshStandardMaterial({ color: colour, roughness, metalness: 0 });
}

export function createMaterials() {
    return {
        brick: new THREE.MeshStandardMaterial({ map: brickTexture(), roughness: 0.95, metalness: 0 }),
        brickArch: matte(Palette.brickArch),
        tiles: new THREE.MeshStandardMaterial({ map: tileTexture(), roughness: 0.85, metalness: 0 }),
        ridge: matte(Palette.ridge, 0.8),
        frameWhite: matte(Palette.frameWhite, 0.5),
        frameDark: matte(Palette.frameDark, 0.5),
        glass: new THREE.MeshPhysicalMaterial({
            color: Palette.glass, transparent: true, opacity: 0.55, roughness: 0.05, metalness: 0,
            envMapIntensity: 1.4, side: THREE.DoubleSide
        }),
        reveal: matte(Palette.reveal),
        zinc: new THREE.MeshStandardMaterial({ color: Palette.zinc, roughness: 0.55, metalness: 0.4, side: THREE.DoubleSide }),
        door: matte(Palette.door, 0.6),
        garageDoor: matte(Palette.garageDoor, 0.6),
        fascia: matte(Palette.fascia, 0.6),
        gutter: matte(Palette.gutter, 0.6),
        lawn: matte(Palette.lawn, 1),
        paving: matte(Palette.paving, 1),
        fence: matte(Palette.fence, 1),
        neighbour: matte(Palette.neighbour, 1),
        scaffoldTube: new THREE.MeshStandardMaterial({ color: Palette.scaffoldTube, roughness: 0.45, metalness: 0.6 }),
        scaffoldBoards: matte(Palette.scaffoldBoards, 1),
        owned: []
    };
}

// A builder that makes a material of its own (a fading patch, a cloned pane) parks it in
// `owned` so it goes when the model goes.
export function disposeMaterials(materials) {
    const { owned, ...shared } = materials;
    for (const material of [...Object.values(shared), ...owned]) {
        material.map?.dispose();
        material.dispose();
    }
}
