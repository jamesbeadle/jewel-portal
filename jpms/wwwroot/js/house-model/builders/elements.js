// The internal works (2026-09-16): each element a footprint polygon extruded from a base to a
// top height, coloured by trade and stamped with the phase it joins and leaves in. Extruded
// prisms cannot make a roof or a dormer — those stay house parts; elements are what is inside.
import * as THREE from "three";
import { stampPhase, phaseOfElement } from "../phases.js";

const FallbackTradeColour = "#9aa0a6";

function tradeMaterial(trade, definition, materials) {
    const key = `trade:${trade}`;
    materials.byTrade ??= new Map();
    if (!materials.byTrade.has(key)) {
        const colour = definition.trades?.find(candidate => candidate.key === trade)?.colour ?? FallbackTradeColour;
        const material = new THREE.MeshStandardMaterial({ color: colour, roughness: 0.85, metalness: 0 });
        materials.owned.push(material);
        materials.byTrade.set(key, material);
    }
    return materials.byTrade.get(key);
}

// Drawn in (x, −z), extruded along the shape's own z, a quarter turn about x lands it along y.
export function extrudeFootprint(footprint, base, top) {
    const shape = new THREE.Shape();
    footprint.forEach(([x, z], index) => (index === 0 ? shape.moveTo(x, -z) : shape.lineTo(x, -z)));
    shape.closePath();
    const geometry = new THREE.ExtrudeGeometry(shape, { depth: Math.max(top - base, 0.005), bevelEnabled: false });
    geometry.rotateX(-Math.PI / 2);
    geometry.translate(0, base, 0);
    return geometry;
}

export function buildElement(element, definition, materials) {
    const geometry = extrudeFootprint(element.footprint, element.base, element.top);
    const mesh = new THREE.Mesh(geometry, tradeMaterial(element.trade, definition, materials));
    mesh.name = element.id;
    mesh.userData.element = element;
    return stampPhase(mesh, phaseOfElement(element, definition.phases));
}

export function buildElements(definition, materials) {
    const group = new THREE.Group();
    group.name = "internal works";
    for (const element of definition.elements ?? []) group.add(buildElement(element, definition, materials));
    return group;
}
