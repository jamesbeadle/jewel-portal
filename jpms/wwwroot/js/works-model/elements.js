// The element list built as meshes, each carrying its element and its trade material, with a
// marker over any element a variation drove.
import * as THREE from "three";
import { extrudeFootprint } from "../house-model/builders/elements.js";
import { tradeMaterial } from "./materials.js";
import { buildVariationMarker } from "./shell.js";

const MarkerAccent = "#f5a623";

export function buildWorksElements(definition, materials) {
    const group = new THREE.Group();
    group.name = "works";
    for (const element of definition.elements ?? []) {
        const mesh = new THREE.Mesh(extrudeFootprint(element.footprint, element.base, element.top), tradeMaterial(materials, element.trade));
        mesh.name = element.id;
        mesh.userData.element = element;
        mesh.castShadow = true;
        mesh.receiveShadow = true;
        group.add(mesh);
        if (element.variationRef) group.add(buildVariationMarker(element, MarkerAccent));
    }
    return group;
}

export function elementMeshes(group) {
    return group.children.filter(child => child.userData.element);
}
