// The works view's own materials: one per trade from the definition's colours, the ghost red
// for what the works remove, the translucent shell the elements sit inside, and a highlight
// for the element under the pointer's click.
import * as THREE from "three";

export const GhostColour = "#c0392b";
const ShellColour = "#d9d6cf";
const HighlightEmissive = "#ffcc55";
const FallbackTradeColour = "#9aa0a6";

export function createWorksMaterials(definition) {
    const byTrade = new Map();
    for (const trade of definition.trades ?? []) {
        byTrade.set(trade.key, new THREE.MeshStandardMaterial({ color: trade.colour ?? FallbackTradeColour, roughness: 0.85, metalness: 0 }));
    }
    return {
        byTrade,
        fallback: new THREE.MeshStandardMaterial({ color: FallbackTradeColour, roughness: 0.85, metalness: 0 }),
        ghost: new THREE.MeshStandardMaterial({ color: GhostColour, transparent: true, opacity: 0.35, roughness: 0.9, metalness: 0, depthWrite: false }),
        shell: new THREE.MeshStandardMaterial({ color: ShellColour, transparent: true, opacity: 0.18, roughness: 0.9, metalness: 0, depthWrite: false, side: THREE.DoubleSide }),
        shellSolid: new THREE.MeshStandardMaterial({ color: ShellColour, roughness: 0.9, metalness: 0 }),
        highlightEmissive: new THREE.Color(HighlightEmissive)
    };
}

export function tradeMaterial(materials, trade) {
    return materials.byTrade.get(trade) ?? materials.fallback;
}

export function disposeWorksMaterials(materials) {
    for (const material of [...materials.byTrade.values(), materials.fallback, materials.ghost, materials.shell, materials.shellSolid]) material.dispose();
}
