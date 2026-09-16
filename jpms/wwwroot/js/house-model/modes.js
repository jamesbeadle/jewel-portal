// The two ways to see the build-up's model — the house as it stands and with the works
// proposed — as one visibility rule over every part's phase.
import { applyPhase } from "./phases.js";

export const Modes = { existing: "existing", proposed: "proposed" };

export function applyMode(state, mode) {
    state.mode = mode === Modes.existing ? Modes.existing : Modes.proposed;
    applyPhase(state.house, state.mode !== Modes.existing);
    state.needsFrame = true;
}
