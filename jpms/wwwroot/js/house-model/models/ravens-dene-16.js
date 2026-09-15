// The demo model: 16 Ravens Dene, Chislehurst BR7 5FL — the house as it stands and the works
// proposed, in one definition the viewer switches between. A massing model read off planning
// drawings at 1:100, not a survey: good to a few centimetres where a dimension was given, to
// a brick or two where it was scaled.
import { blocks, faces, existingOpenings, downpipes, context } from "./ravens-dene-16-house.js";
import { rooflights, dormer, proposedOpenings, infills } from "./ravens-dene-16-proposal.js";
import { programme } from "./ravens-dene-16-programme.js";

export const ravensDene16 = {
    key: "ravens-dene-16",
    name: "16 Ravens Dene, Chislehurst",
    source: "Resi planning drawings B369214 rev B, 4 September 2026",
    centre: [4.3, 3.4, 5.6],
    blocks,
    faces,
    openings: [...existingOpenings, ...proposedOpenings],
    infills,
    rooflights,
    dormer,
    downpipes,
    context,
    programme
};
