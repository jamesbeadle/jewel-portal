// The three ways to see the model. Existing and proposed are the phase toggle over the whole
// house; works lifts the shell off the finished house so the internal elements can be seen.
import { applyPhase } from "./phases.js";
import { isShell } from "./house-builder.js";

export const Modes = { existing: "existing", proposed: "proposed", works: "works" };

const known = new Set(Object.values(Modes));

export function applyMode(state, mode) {
    const works = mode === Modes.works;
    state.mode = known.has(mode) ? mode : Modes.proposed;
    applyPhase(state.house, state.mode !== Modes.existing);
    state.house.traverse(part => { if (isShell(part)) part.visible = !works; });
    state.elements.visible = works;
    state.needsFrame = true;
}
