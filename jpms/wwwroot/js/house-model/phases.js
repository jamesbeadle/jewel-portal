// What each part of the model belongs to: the house as it stands, the works proposed, what the
// works take away, or the temporary works — scaffold, stripped roof — seen only while the
// build-up plays. Showing the proposal hides what is removed; showing the existing house hides
// the proposal; construction parts are hidden in both. Parts with no phase are the house itself.
export const Phase = {
    existing: "existing",
    proposed: "proposed",
    removed: "removed",
    construction: "construction"
};

export function stampPhase(object, phase) {
    object.userData.phase = phase ?? Phase.existing;
    return object;
}

export function applyPhase(root, isProposed) {
    root.traverse(object => {
        const phase = object.userData.phase;
        if (phase === undefined) return;
        object.visible = phase === Phase.existing || phase === (isProposed ? Phase.proposed : Phase.removed);
    });
}

// Joining in the first phase is existing (removed if it leaves); later is proposed (construction if it leaves).
export function phaseOfElement(element, phases) {
    const first = phases?.[0]?.key;
    const joinsAtStart = element.phaseIn === undefined || element.phaseIn === null || element.phaseIn === first;
    const leaves = element.phaseOut !== undefined && element.phaseOut !== null;
    if (joinsAtStart) return leaves ? Phase.removed : Phase.existing;
    return leaves ? Phase.construction : Phase.proposed;
}
