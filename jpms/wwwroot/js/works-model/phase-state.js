// Where an element stands at a phase. It has joined once the phase it joins in has been
// reached, and left once the phase it leaves in has; the house parts (the shell, the dormer,
// the rooflights) follow the same reading through the phase their own stamp maps to.
import { Phase } from "../house-model/phases.js";

export const ShellPhases = { proposedJoinsAt: "03", removedLeavesAt: "01" };

export function phaseIndex(phases, key) {
    if (key === undefined || key === null) return null;
    const index = phases.findIndex(phase => phase.key === key);
    return index < 0 ? null : index;
}

export function standingOf(element, phases, current) {
    const joinsAt = phaseIndex(phases, element.phaseIn) ?? 0;
    const leavesAt = phaseIndex(phases, element.phaseOut);
    return {
        isJoined: joinsAt <= current,
        hasLeft: leavesAt !== null && leavesAt <= current,
        isNew: joinsAt > 0,
        joinsAt,
        leavesAt
    };
}

// A house part's stamp read as an element would be: existing joins at the start and stays,
// proposed joins at the envelope phase, removed leaves at strip out, construction is not shown.
export function shellPartAsElement(phase) {
    if (phase === Phase.proposed) return { phaseIn: ShellPhases.proposedJoinsAt, phaseOut: null };
    if (phase === Phase.removed) return { phaseIn: undefined, phaseOut: ShellPhases.removedLeavesAt };
    if (phase === Phase.construction) return null;
    return { phaseIn: undefined, phaseOut: null };
}
