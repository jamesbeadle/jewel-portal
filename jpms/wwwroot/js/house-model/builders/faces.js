// Where an opening sits on a wall. A face is one outside wall named in the model definition —
// its fixed axis, where along that axis it stands, and which way it looks — and an opening is
// placed by its coordinate along the face and its height, turned to look outward with it.
export function placeOnFace(object, face, along, height) {
    if (face.axis === "z") {
        object.position.set(along, height, face.at);
        object.rotation.y = face.outward > 0 ? 0 : Math.PI;
        return object;
    }
    object.position.set(face.at, height, along);
    object.rotation.y = face.outward > 0 ? Math.PI / 2 : -Math.PI / 2;
    return object;
}
