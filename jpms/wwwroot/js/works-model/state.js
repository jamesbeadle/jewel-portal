// Everything a mounted works model holds, built once at mount: the scene's parts, the phase
// the scrubber is on, the trades hidden, the switches, and the callbacks into the page.
import { tradeMaterial } from "./materials.js";

export function createState({ host, dotnetRef, definition, stage, materials, houseMaterials, shell, works }) {
    return {
        host, dotnetRef, definition, stage, materials, houseMaterials, shell, works,
        phase: 0,
        hiddenTrades: new Set(),
        isGhosting: true,
        showsShell: true,
        picked: null,
        recorder: null,
        isRecordingUntilFinished: false,
        needsFrame: true,
        disposed: false,
        tradeMaterialOf: trade => tradeMaterial(materials, trade),
        onPhase: index => dotnetRef.invokeMethodAsync("PhaseChanged", index).catch(() => {})
    };
}
