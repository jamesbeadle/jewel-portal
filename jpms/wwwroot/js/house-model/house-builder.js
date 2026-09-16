// Turns a model definition into the house: each block's walls, gable and roof, then every
// opening on its face, the new brickwork, every rooflight on its slope, the dormer, and the
// downpipes — each part stamped with the phase it belongs to so the proposal can be switched
// on and off — and, for the build-up, the temporary works the programme calls for. The
// internal works (definition.elements) are builders/elements.js, added by the viewer.
import * as THREE from "three";
import { wallBlock, gablePrism } from "./builders/wall-blocks.js";
import { gableRoof } from "./builders/gable-roof.js";
import { buildOpening } from "./builders/openings.js";
import { buildBrickInfill } from "./builders/brick-infill.js";
import { buildRooflight } from "./builders/rooflights.js";
import { buildGlazedDormer } from "./builders/glazed-dormer.js";
import { buildDownpipe } from "./builders/rainwater.js";
import { buildScaffold } from "./builders/scaffold.js";
import { buildRoofStrip } from "./builders/roof-strip.js";
import { stampPhase } from "./phases.js";
import { castsAndReceivesShadows } from "./shadows.js";

function blockNamed(definition, name) {
    return definition.blocks.find(block => block.name === name);
}

const Shell = "shell";

export function isShell(part) {
    return part.userData.role === Shell;
}

function asShell(part) {
    part.userData.role = Shell;
    return part;
}

export function buildHouse(definition, materials) {
    const house = new THREE.Group();
    house.name = definition.name;
    for (const block of definition.blocks) {
        house.add(asShell(wallBlock(block, materials)), asShell(gablePrism(block, materials)), asShell(gableRoof(block, materials)));
    }
    for (const opening of definition.openings ?? []) {
        house.add(stampPhase(buildOpening(opening, definition.faces, materials), opening.phase));
    }
    for (const infill of definition.infills ?? []) {
        house.add(stampPhase(buildBrickInfill(infill, definition.faces, materials), infill.phase));
    }
    for (const rooflight of definition.rooflights ?? []) {
        house.add(stampPhase(buildRooflight(rooflight, blockNamed(definition, rooflight.block), materials), rooflight.phase));
    }
    if (definition.dormer) {
        house.add(stampPhase(buildGlazedDormer(definition.dormer, materials), definition.dormer.phase));
    }
    for (const downpipe of definition.downpipes ?? []) house.add(buildDownpipe(downpipe, materials));
    return castsAndReceivesShadows(house);
}

export function buildTemporaryWorks(definition, materials) {
    const works = new THREE.Group();
    works.name = "temporary works";
    const { scaffold, roofStrip } = definition.programme ?? {};
    if (scaffold) works.add(buildScaffold(scaffold, materials));
    if (roofStrip) works.add(stampPhase(buildRoofStrip(roofStrip, blockNamed(definition, roofStrip.block), materials), roofStrip.phase));
    return castsAndReceivesShadows(works);
}
