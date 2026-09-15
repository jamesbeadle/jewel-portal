// A box whose texture coordinates are metres rather than 0..1 per face, so a brick or tile
// pattern lands at true scale on a 5.7 m wall and a 9.9 m wall alike. BoxGeometry lays its six
// faces out in a fixed order — +x, −x, +y, −y, +z, −z — with four corners each.
import * as THREE from "three";

const CornersPerFace = 4;

export function boxInMetres(width, height, depth) {
    const geometry = new THREE.BoxGeometry(width, height, depth);
    const faceSizes = [
        [depth, height], [depth, height],
        [width, depth], [width, depth],
        [width, height], [width, height]
    ];
    const uv = geometry.getAttribute("uv");
    faceSizes.forEach(([faceWidth, faceHeight], face) => {
        for (let corner = 0; corner < CornersPerFace; corner++) {
            const index = face * CornersPerFace + corner;
            uv.setXY(index, uv.getX(index) * faceWidth, uv.getY(index) * faceHeight);
        }
    });
    return geometry;
}

export function boxMesh(width, height, depth, material) {
    return new THREE.Mesh(boxInMetres(width, height, depth), material);
}
