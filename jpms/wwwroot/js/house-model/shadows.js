// Everything solid throws a shadow and takes one; glass, being see-through, takes shadows but
// throws none (the sun's shadow map cannot see through a pane, so a casting pane would print a
// black rectangle wherever it fell).
export function castsAndReceivesShadows(root) {
    root.traverse(part => {
        if (!part.isMesh) return;
        part.castShadow = !part.material.transparent;
        part.receiveShadow = true;
    });
    return root;
}
