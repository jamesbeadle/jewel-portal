// The works Resi drew for 16 Ravens Dene: the glazed dormer on the rear slope (six VELUX GPU
// MK08 units — three steep on the front, three on the low-pitched top, 2.88 m wide, 2.01 m
// tall), three GGU MK06 rooflights on the front slope and two on the garage's rear slope, and
// the garage turned into a TV room — its door bricked up, windows to the garden.
import { Phase } from "../phases.js";

const RooflightWidth = 0.78;
const RooflightLength = 1.18;

const frontRooflight = (reference, x) => ({
    reference, block: "house", side: "front", x, sillHeight: 7.03,
    width: RooflightWidth, length: RooflightLength, phase: Phase.proposed
});

const garageRooflight = (reference, x) => ({
    reference, block: "garage", side: "rear", x, sillHeight: 3.0,
    width: RooflightWidth, length: RooflightLength, phase: Phase.proposed
});

export const rooflights = [
    frontRooflight("R11", 1.38),
    frontRooflight("R10", 2.78),
    frontRooflight("R9", 4.16),
    garageRooflight("R7", 6.3),
    garageRooflight("R8", 7.8)
];

export const dormer = {
    x: [1.46, 4.34],
    profile: [{ z: 8.87, y: 5.89 }, { z: 8.37, y: 7.20 }, { z: 6.60, y: 7.85 }],
    columns: 3,
    phase: Phase.proposed
};

export const proposedOpenings = [
    { name: "utility window", face: "garageFront", kind: "window", u: [6.15, 6.95], y: [1.1, 2.0], columns: 1, phase: Phase.proposed },
    { name: "garden window 1", face: "garageRear", kind: "window", u: [5.88, 6.72], y: [0.84, 2.23], columns: 1, phase: Phase.proposed },
    { name: "garden window 2", face: "garageRear", kind: "window", u: [7.36, 8.25], y: [0.84, 2.23], columns: 1, phase: Phase.proposed }
];

export const infills = [
    { name: "garage infill", face: "garageFront", u: [6.0, 8.4], y: [0, 2.1], phase: Phase.proposed }
];
