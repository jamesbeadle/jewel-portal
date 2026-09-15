// A framed pane of glass: an outer frame, mullions and transoms dividing it into columns and
// rows, and the glass set back inside. Built flat in its own x–y plane facing +z, so a window,
// a rooflight and a dormer face are all this one unit turned to sit where they belong.
import * as THREE from "three";

export function glazingUnit({ width, height, columns = 1, rows = 1, frameWidth = 0.07, frameDepth = 0.06 },
    frameMaterial, glassMaterial) {
    const unit = new THREE.Group();
    const bar = (barWidth, barHeight, x, y) => {
        const piece = new THREE.Mesh(new THREE.BoxGeometry(barWidth, barHeight, frameDepth), frameMaterial);
        piece.position.set(x, y, frameDepth / 2);
        unit.add(piece);
    };
    bar(width, frameWidth, 0, height / 2 - frameWidth / 2);
    bar(width, frameWidth, 0, -height / 2 + frameWidth / 2);
    bar(frameWidth, height, -width / 2 + frameWidth / 2, 0);
    bar(frameWidth, height, width / 2 - frameWidth / 2, 0);
    const innerWidth = width - 2 * frameWidth;
    const innerHeight = height - 2 * frameWidth;
    for (let column = 1; column < columns; column++) {
        bar(frameWidth, innerHeight, -innerWidth / 2 + (innerWidth * column) / columns, 0);
    }
    for (let row = 1; row < rows; row++) {
        bar(innerWidth, frameWidth, 0, -innerHeight / 2 + (innerHeight * row) / rows);
    }
    const glass = new THREE.Mesh(new THREE.PlaneGeometry(innerWidth, innerHeight), glassMaterial);
    glass.position.z = frameDepth * 0.35;
    unit.add(glass);
    return unit;
}
