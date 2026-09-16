// The house the works happen inside, drawn quietly: the same parts the build-up builds (walls,
// roof, openings, dormer, rooflights, infill), re-clothed in one translucent material and
// stamped with the phase each joins or leaves in, so the scrubber moves them too. The ground,
// paving and fences stay for bearings; the neighbours are left out so they never hide the works.
import * as THREE from "three";
import { buildHouse } from "../house-model/house-builder.js";
import { createMaterials } from "../house-model/materials.js";
import { buildContext } from "../house-model/builders/context.js";
import { shellPartAsElement } from "./phase-state.js";

export function buildShell(definition, materials) {
    const houseMaterials = createMaterials();
    const shell = buildHouse(definition, houseMaterials);
    shell.name = "shell";
    shell.traverse(part => {
        if (part.isMesh) { part.material = materials.shell; part.castShadow = false; part.receiveShadow = false; }
    });
    for (const part of shell.children) {
        const asElement = shellPartAsElement(part.userData.phase);
        part.userData.standing = asElement;
        part.visible = asElement !== null;
    }
    const context = buildContext({ ...(definition.context ?? {}), neighbours: [] }, houseMaterials);
    return { shell, context, houseMaterials };
}

// A marker over an element a variation drove: a pole from its top to a small flag.
export function buildVariationMarker(element, accent) {
    const xs = element.footprint.map(([x]) => x);
    const zs = element.footprint.map(([, z]) => z);
    const centreX = (Math.min(...xs) + Math.max(...xs)) / 2;
    const centreZ = (Math.min(...zs) + Math.max(...zs)) / 2;
    const material = new THREE.MeshBasicMaterial({ color: accent });
    const pole = new THREE.Mesh(new THREE.CylinderGeometry(0.02, 0.02, 0.8), material);
    pole.position.set(centreX, element.top + 0.4, centreZ);
    const flag = new THREE.Mesh(new THREE.BoxGeometry(0.3, 0.18, 0.02), material);
    flag.position.set(centreX + 0.15, element.top + 0.72, centreZ);
    const marker = new THREE.Group();
    marker.add(pole, flag);
    marker.name = `variation marker ${element.id}`;
    return marker;
}
