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
