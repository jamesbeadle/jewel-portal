// The named viewpoints, each a camera position and the point it looks at, set out from the
// model's centre. The four a person can pick sit around the house; the build-up adds closer
// ones on the work in hand — the rear slope for the dormer, the front slope for its rooflights,
// the garage from the road for its door and from the garden for its windows. Overview is the opening view: from the garden side, high
// enough to take in the roof and the garage together. Above stands a touch to the road side so
// the plan reads as the drawings do, road at the foot.
import * as THREE from "three";

export const ViewNames = {
    overview: "overview",
    garden: "garden",
    road: "road",
    side: "side",
    above: "above",
    dormer: "dormer",
    frontRoof: "frontRoof",
    garageFront: "garageFront",
    garageGarden: "garageGarden"
};

export function houseViews(definition) {
    const centre = new THREE.Vector3(...definition.centre);
    const from = (offset, lookAt = [0, 0, 0]) => ({
        position: centre.clone().add(new THREE.Vector3(...offset)),
        target: centre.clone().add(new THREE.Vector3(...lookAt))
    });
    return {
        [ViewNames.overview]: from([14, 9.5, 18]),
        [ViewNames.garden]: from([0.5, 2.5, 21]),
        [ViewNames.road]: from([-1, 2.5, -21]),
        [ViewNames.side]: from([19, 7, 11]),
        [ViewNames.above]: from([0, 28, -6]),
        [ViewNames.dormer]: from([3.5, 5.5, 13.5], [-1.4, 3.3, 2.1]),
        [ViewNames.frontRoof]: from([-2.5, 6.5, -15.5], [-1.5, 3.6, -3.1]),
        [ViewNames.garageFront]: from([6, 3.2, -11.5], [2.9, -1.8, -1.3]),
        [ViewNames.garageGarden]: from([10, 5, 15], [2.9, -0.9, 5.3])
    };
}
