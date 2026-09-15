// The brick and tile surfaces, drawn on a canvas rather than shipped as images: a running-bond
// brick tile 0.9 m square (four 215 mm bricks, twelve 75 mm courses) and a plain-tile roof
// 1 m square (three 333 mm courses, staggered). Every wall and slope carries UVs in metres, so
// one repeat setting here scales the pattern correctly on every surface.
import * as THREE from "three";
import { Palette, BrickPatternMetres, TilePatternMetres } from "./palette.js";

const CanvasSize = 512;

function seededPicker(seed) {
    let state = seed;
    return function pick(list) {
        state = (state * 1103515245 + 12345) % 2147483648;
        return list[state % list.length];
    };
}

function repeatingTexture(canvas, patternMetres) {
    const texture = new THREE.CanvasTexture(canvas);
    texture.wrapS = THREE.RepeatWrapping;
    texture.wrapT = THREE.RepeatWrapping;
    texture.colorSpace = THREE.SRGBColorSpace;
    texture.repeat.set(1 / patternMetres, 1 / patternMetres);
    texture.anisotropy = 8;
    return texture;
}

function canvasOf(background) {
    const canvas = document.createElement("canvas");
    canvas.width = CanvasSize;
    canvas.height = CanvasSize;
    const context = canvas.getContext("2d");
    context.fillStyle = background;
    context.fillRect(0, 0, CanvasSize, CanvasSize);
    return { canvas, context };
}

export function brickTexture() {
    const { canvas, context } = canvasOf(Palette.mortar);
    const courses = 12;
    const bricksPerCourse = 4;
    const courseHeight = CanvasSize / courses;
    const brickWidth = CanvasSize / bricksPerCourse;
    const joint = 5;
    const pick = seededPicker(7);
    for (let course = 0; course < courses; course++) {
        const offset = course % 2 === 0 ? 0 : brickWidth / 2;
        for (let brick = -1; brick <= bricksPerCourse; brick++) {
            context.fillStyle = pick(Palette.brickTones);
            context.fillRect(brick * brickWidth + offset + joint / 2, course * courseHeight + joint / 2,
                brickWidth - joint, courseHeight - joint);
        }
    }
    return repeatingTexture(canvas, BrickPatternMetres);
}

export function tileTexture() {
    const { canvas, context } = canvasOf(Palette.tileShadow);
    const courses = 3;
    const tilesPerCourse = 3;
    const courseHeight = CanvasSize / courses;
    const tileWidth = CanvasSize / tilesPerCourse;
    const shadowLine = 7;
    const seam = 2;
    const pick = seededPicker(3);
    for (let course = 0; course < courses; course++) {
        const offset = course % 2 === 0 ? 0 : tileWidth / 2;
        for (let tile = -1; tile <= tilesPerCourse; tile++) {
            context.fillStyle = pick(Palette.tileTones);
            context.fillRect(tile * tileWidth + offset + seam / 2, course * courseHeight,
                tileWidth - seam, courseHeight - shadowLine);
        }
    }
    return repeatingTexture(canvas, TilePatternMetres);
}
