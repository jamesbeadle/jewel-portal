// 16 Ravens Dene as it stands, read off Resi's planning drawings B369214 rev B (4 Sep 2026):
// a red-brick semi, party wall to the left seen from the road, gable roof with its ridge along
// the front (ridge 9.27 m, eaves at the first-floor ceiling), and the single-storey garage
// tucked against its right flank at the rear. Metres; x along the front from the party wall,
// z from the front wall towards the garden, heights above the ground-floor finished floor.
import { Phase } from "../phases.js";

const House = { name: "house", x: 0, z: 0, width: 5.7, depth: 9.9, eaves: 5.0,
    roof: { ridge: 9.27, overhang: 0.3, vergeLeft: 0, vergeRight: 0.25 } };
const Garage = { name: "garage", x: 5.7, z: 4.3, width: 3.0, depth: 6.6, eaves: 2.4,
    roof: { ridge: 4.75, overhang: 0.25, vergeLeft: 0, vergeRight: 0.2 } };

export const blocks = [House, Garage];

export const faces = {
    front: { axis: "z", at: House.z, outward: -1 },
    rear: { axis: "z", at: House.z + House.depth, outward: 1 },
    right: { axis: "x", at: House.x + House.width, outward: 1 },
    garageFront: { axis: "z", at: Garage.z, outward: -1 },
    garageRear: { axis: "z", at: Garage.z + Garage.depth, outward: 1 }
};

export const existingOpenings = [
    { face: "front", kind: "window", u: [1.10, 2.47], y: [0.97, 2.14], columns: 3, arched: true },
    { face: "front", kind: "door", u: [3.65, 4.66], y: [0, 1.93], canopy: true },
    { face: "front", kind: "window", u: [1.10, 2.47], y: [3.26, 4.53], columns: 3, arched: true },
    { face: "front", kind: "window", u: [3.70, 4.56], y: [3.26, 4.53], columns: 2, arched: true },
    { face: "rear", kind: "slidingDoors", u: [1.15, 4.56], y: [0, 2.22], columns: 2 },
    { face: "rear", kind: "window", u: [3.77, 4.61], y: [3.46, 4.50], columns: 2, arched: true },
    { face: "rear", kind: "window", u: [1.35, 2.68], y: [3.46, 4.50], columns: 3, arched: true },
    { face: "right", kind: "window", u: [1.55, 2.27], y: [1.13, 2.16], columns: 1, arched: true },
    { face: "right", kind: "window", u: [4.63, 5.35], y: [5.60, 6.65], columns: 1, arched: true },
    { name: "garage door", face: "garageFront", kind: "garageDoor", u: [6.0, 8.4], y: [0, 2.1], phase: Phase.removed }
];

export const downpipes = [
    { x: 5.55, z: -0.08, top: 4.75 },
    { x: 5.55, z: 9.98, top: 4.75 },
    { x: 8.62, z: 4.22, top: 2.15 },
    { x: 8.62, z: 10.98, top: 2.15 }
];

export const context = {
    paving: [
        { x: [0, 8.85], z: [-7, 0] },
        { x: [5.7, 8.85], z: [0, 4.3] },
        { x: [0, 5.7], z: [9.9, 12.6] },
        { x: [5.7, 8.85], z: [10.9, 12.6] }
    ],
    fences: [
        { from: [8.82, 10.9], to: [8.82, 24] },
        { from: [-6.2, 24], to: [8.82, 24] },
        { from: [0, 9.9], to: [0, 24] },
        { from: [-6.2, 9.6], to: [-6.2, 24] }
    ],
    neighbours: [
        { name: "attached", x: -6.2, z: 0.3, width: 6.2, depth: 9.3, eaves: 4.9,
            roof: { ridge: 8.3, overhang: 0.25, vergeLeft: 0.25, vergeRight: 0 } },
        { name: "detached", x: 10.6, z: 0.6, width: 6.6, depth: 9.0, eaves: 5.0,
            roof: { ridge: 8.4, overhang: 0.25, vergeLeft: 0.25, vergeRight: 0.25 } }
    ]
};
