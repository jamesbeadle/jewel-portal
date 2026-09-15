// A roof window on a slope: the dark frame standing a little proud of the tiles with the glass
// inside it, as a VELUX sits. It is placed by the height of its bottom edge above the floor —
// the figure the elevation gives — and its position along the eaves.
import * as THREE from "three";
import { glazingUnit } from "./glazing-unit.js";
import { slopeFrame, orientToSlope } from "./gable-roof.js";

const FrameWidth = 0.06;
const FrameDepth = 0.1;

export function buildRooflight(rooflight, block, materials) {
    const frame = slopeFrame(block, rooflight.side);
    const unit = glazingUnit(
        { width: rooflight.width, height: rooflight.length, frameWidth: FrameWidth, frameDepth: FrameDepth },
        materials.frameDark, materials.glass);
    unit.rotation.x = -Math.PI / 2;
    const placed = new THREE.Group();
    placed.add(unit);
    placed.name = `rooflight ${rooflight.reference}`;
    orientToSlope(placed, frame);
    const alongSlopeToSill = (rooflight.sillHeight - block.eaves) / Math.sin(frame.pitch);
    placed.position.copy(frame.origin)
        .addScaledVector(frame.up, alongSlopeToSill + rooflight.length / 2)
        .setX(rooflight.x);
    return placed;
}
