// Moves a group's origin to a chosen point without moving anything in it, so a scale or a turn
// happens about that point — a dormer rising from its sill rather than from the site origin.
// Only for a group not yet turned or scaled.
import * as THREE from "three";

export function anchorAt(group, point) {
    if (group.userData.isAnchored) return group;
    const anchor = new THREE.Vector3(...point);
    for (const child of group.children) child.position.sub(anchor);
    group.position.copy(anchor);
    group.userData.isAnchored = true;
    return group;
}
