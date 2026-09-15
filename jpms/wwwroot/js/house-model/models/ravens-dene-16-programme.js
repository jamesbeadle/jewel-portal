// The programme of works for 16 Ravens Dene as the build-up plays it: what goes up, in what
// order, seen from where, with the words the page shows for each stage. Times in
// milliseconds; `at` is from the start of the stage, `hold` is the pause after its last move.
import { Phase } from "../phases.js";

const RooflightDrop = [0, 1.2, 0];
const IntoTheFront = [0, 0, -0.6];
const IntoTheRear = [0, 0, 0.6];

const dropIn = (part, at, from = RooflightDrop) => ({ part, kind: "drop", from, at, duration: 1000 });

export const programme = {
    scaffold: { standardsAt: [-0.1, 1.85, 3.8, 5.75], rowsAt: [10.35, 11.35], lifts: 3, phase: Phase.construction },
    roofStrip: { name: "roof strip", block: "house", side: "rear", x: [1.31, 4.49], alongSlope: [1.15, 4.6], phase: Phase.construction },
    stages: [
        { title: "As it stands", view: "overview", hold: 5000,
            detail: "Four bedrooms over two floors, an integral garage to the side, and a loft that has never been used." },
        { title: "Scaffold the garden side", view: "garden", hold: 1500,
            detail: "Three lifts along the rear, up to the eaves — the roof works are done from here.",
            steps: [
                { part: "scaffold standards", kind: "grow", duration: 2600 },
                dropIn("scaffold lift 1", 2700, [0, 0.4, 0]),
                dropIn("scaffold lift 2", 3500, [0, 0.4, 0]),
                dropIn("scaffold lift 3", 4300, [0, 0.4, 0])
            ] },
        { title: "Open up the roof", view: "dormer", hold: 2200,
            detail: "The tiles come off where the dormer will sit, back to the battens, and the new loft floor goes in at 5.33 m.",
            steps: [{ part: "roof strip", kind: "fade", duration: 1800 }] },
        { title: "Raise the dormer", view: "dormer", hold: 1800,
            detail: "The frame goes up over the opening — 2.88 m wide, a steep front and a low-pitched top — closed with zinc cheeks either side.",
            steps: [{ part: "glazed dormer", kind: "grow", anchor: [2.9, 5.89, 8.87], duration: 3200 }] },
        { title: "Glaze it", view: "dormer", hold: 2600,
            detail: "Six VELUX GPU windows: three across the front, three across the top.",
            steps: [{ part: "glazed dormer", kind: "fade", duration: 1800 }] },
        { title: "Rooflights to the front", view: "frontRoof", hold: 2400,
            detail: "Three VELUX GGU centre-pivot rooflights on the front slope light the landing and the front bedroom.",
            steps: [dropIn("rooflight R11", 0), dropIn("rooflight R10", 900), dropIn("rooflight R9", 1800)] },
        { title: "The garage comes out of use", view: "garageFront", hold: 2200,
            detail: "The up-and-over door comes out and the opening is bricked up, with a window for the new utility room.",
            steps: [
                { part: "garage door", kind: "lift", by: [0, 2.3, 0], duration: 1800 },
                { part: "garage infill", kind: "grow", at: 1900, duration: 2300 },
                dropIn("utility window", 4400, IntoTheFront)
            ] },
        { title: "…and opens to the garden", view: "garageGarden", hold: 2600,
            detail: "Two windows and two rooflights to the garden: the TV room, with the utility and WC behind it.",
            steps: [
                dropIn("garden window 1", 0, IntoTheRear),
                dropIn("garden window 2", 700, IntoTheRear),
                dropIn("rooflight R7", 1500),
                dropIn("rooflight R8", 2200)
            ] },
        { title: "Scaffold down", view: "overview", hold: 1200,
            detail: "Re-tiled around the dormer, the scaffold comes down.",
            steps: [
                { part: "roof strip", kind: "vanish" },
                { part: "scaffold lift 3", kind: "vanish", at: 300 },
                { part: "scaffold lift 2", kind: "vanish", at: 800 },
                { part: "scaffold lift 1", kind: "vanish", at: 1300 },
                { part: "scaffold standards", kind: "shrink", at: 1500, duration: 2000 }
            ] },
        { title: "Finished — look around", view: "overview", hold: 0,
            detail: "Drag to orbit, scroll to zoom, right-drag to move — or take one of the drawings' viewpoints below." }
    ]
};
