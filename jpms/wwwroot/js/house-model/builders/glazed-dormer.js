// The glazed dormer on the rear slope: not a box dormer but a bank of roof windows standing out
// from the roof — a steep glazed front, a low-pitched glazed top, and zinc cheeks closing the
// sides — exactly the profile Section A–A draws. The definition gives the profile as points in
// the z–y plane, from the bottom edge on the roof, over the front and top, back to the roof.
import * as THREE from "three";
import { glazingUnit } from "./glazing-unit.js";

const AlongX = new THREE.Vector3(1, 0, 0);
const FrameWidth = 0.08;
const FrameDepth = 0.08;

function cheek(x, profile, materials) {
    const corners = profile.map(point => new THREE.Vector3(x, point.y, point.z));
    const geometry = new THREE.BufferGeometry().setFromPoints(corners);
    geometry.computeVertexNormals();
    return new THREE.Mesh(geometry, materials.zinc);
}

function glazedFace(from, to, x, width, columns, materials) {
    const start = new THREE.Vector3(x, from.y, from.z);
    const end = new THREE.Vector3(x, to.y, to.z);
    const length = start.distanceTo(end);
    const along = end.clone().sub(start).normalize();
    const outward = AlongX.clone().cross(along);
    const face = glazingUnit({ width, height: length, columns, frameWidth: FrameWidth, frameDepth: FrameDepth },
        materials.frameDark, materials.glass);
    face.quaternion.setFromRotationMatrix(new THREE.Matrix4().makeBasis(AlongX, along, outward));
    face.position.copy(start).lerp(end, 0.5);
    return face;
}

export function buildGlazedDormer(dormer, materials) {
    const [left, right] = dormer.x;
    const width = right - left;
    const centreX = (left + right) / 2;
    const group = new THREE.Group();
    group.name = "glazed dormer";
    group.add(cheek(left, dormer.profile, materials), cheek(right, dormer.profile, materials));
    for (let segment = 0; segment < dormer.profile.length - 1; segment++) {
        group.add(glazedFace(dormer.profile[segment], dormer.profile[segment + 1], centreX, width, dormer.columns, materials));
    }
    return group;
}
