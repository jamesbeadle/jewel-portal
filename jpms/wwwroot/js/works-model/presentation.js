// What the scene shows for a chosen phase: every element and shell part placed by its standing
// — joined and still there, solid; left, gone (or ghosted red when ghost mode is on); not yet
// joined, absent — with hidden trades kept out and the shell kept or dropped as asked. One
// pass, called whenever the phase, the ghost switch, a trade or the shell changes.
import { standingOf } from "./phase-state.js";
import { elementMeshes } from "./elements.js";

export function present(state) {
    const { phases } = state.definition;
    const current = Math.floor(state.phase);
    for (const mesh of elementMeshes(state.works)) presentElement(state, mesh, standingOf(mesh.userData.element, phases, current));
    for (const marker of state.works.children.filter(child => !child.userData.element)) {
        const owner = state.works.getObjectByName(marker.name.replace("variation marker ", ""));
        marker.visible = owner?.visible ?? false;
    }
    for (const part of state.shell.children) presentShellPart(state, part, current);
    state.needsFrame = true;
}

function presentElement(state, mesh, standing) {
    const element = mesh.userData.element;
    const tradeHidden = state.hiddenTrades.has(element.trade);
    if (!standing.isJoined || tradeHidden) { mesh.visible = false; return; }
    if (standing.hasLeft) {
        mesh.visible = state.isGhosting;
        mesh.material = state.materials.ghost;
        return;
    }
    mesh.visible = true;
    mesh.material = state.tradeMaterialOf(element.trade);
}

function presentShellPart(state, part, current) {
    const asElement = part.userData.standing;
    if (asElement === null || !state.showsShell) { part.visible = false; return; }
    const standing = standingOf(asElement, state.definition.phases, current);
    part.visible = standing.isJoined && (!standing.hasLeft || state.isGhosting);
    const material = standing.hasLeft ? state.materials.ghost : state.materials.shell;
    part.traverse(mesh => { if (mesh.isMesh) mesh.material = material; });
}
