---
name: jpms-house-model
description: "How an estimate's 3D model is read off the enquiry's architect drawings and stored on the estimate — the lead page's build-up of the works and its inside-the-works view. Load before drafting, revising or explaining a house model, before set_house_model, and whenever a drawing revision lands on a lead that has one. Encodes drawings-are-the-source, levels-from-the-section, one-definition-two-views, elements-carry-the-estimate's-cost-codes, every-inference-is-noted, and a-person-confirms-before-it-is-stored."
---

# JPMS — The house model

The 3D model on a lead's page (Sales → Leads → the lead → 3D model) is the prospect's own
house and their own works, read off their architect's drawings — never a demo, never a survey.
It plays the works as a build-up (scaffold up, roof opened, dormer raised and glazed,
rooflights in, the garage bricked up, scaffold down), hands the camera over at the end, and
can be turned inside out to show the internal works by trade. One definition feeds both
views. The assistant drafts it; a person checks it; `set_house_model` stores it on the
estimate; the panel shows it in place of "no model drafted yet".

## What the definition is

One JSON object on the estimate (`LeadEstimate.HouseModelJson`; `get_lead` returns it as
`estimates[].houseModel.definition`, with `source` and `setAt`). Its shape is the reference
`definition-schema`; in short:

- **The house as it stands** — `blocks` (one per roofed mass: house, garage, extension; each a
  footprint, eaves and a gable roof with its ridge), `faces` (the elevations openings sit on),
  `openings` (windows and doors by face, cill and head), `downpipes`, `context` (paving,
  fences, neighbours). This is what the viewer's builders can make: brick boxes, gable roofs,
  glazing units, arches, canopies, a glazed dormer, rooflights, brick infill.
- **The works** — openings with `phase: "removed"`, `infills`, `rooflights`, the `dormer`, and
  the temporary works the `programme` needs (scaffold, roof strip).
- **The programme** — the stages the build-up plays, in build order, each naming parts and the
  move each makes (grow / shrink / drop / lift / appear / vanish / fade) with the caption the
  page shows.
- **The internal works** — `phases` (the project's ordered list: 00 Existing, 01 Strip out,
  02 Structure, 03 Envelope, 04 First fix, 05 Board and plaster, 06 Second fix, 07 Finishes —
  data, so a project can change it), `trades` (key, name, colour) and `elements`: each a
  footprint polygon extruded from `base` to `top`, with its trade, `phaseIn`, `phaseOut`
  (null when it stays), `costCode` from the estimate's breakdown, and `variationRef` when a
  variation drove it. Extruded footprints cannot be a pitched roof, a gable or the glazed
  dormer — those stay house parts. Elements are what happens inside them.
- **`source`** — the sheets and revision read, the levels taken from the section, and the
  caveat the caption prints.

Units: metres. `x` along the front from the party wall (or the left-hand boundary seen from
the road), `z` from the front wall towards the garden, heights above the ground-floor finished
floor — exactly as the drawings level. The viewer never looks a model up by name.

## How to read one off the drawings

1. **Read the estimate first.** `get_lead` for the lead: the estimate's scope, its breakdown
   (the cost codes the elements will carry) and `houseModel` if one exists. A drawing revision
   later than the stored `source` means a re-read of the model, not a guess.
2. **Read the sheets.** The enquiry's drawings are on the lead's mail (`read_record_emails`
   record_type `lead`, then `read_email_attachment` / `read_source` page by page). When the
   drawings are on a project's Documents register instead, `get_document_extraction` gives the
   proven scale and `query_document_data` the figured dimensions, callouts and shapes as rows —
   use those figures before anything scaled.
3. **Levels from the section.** Ground floor 0, first floor, ceiling heights, the new loft
   floor, eaves and ridge — the section and the elevations carry them as level lines (`EX_`
   existing, `PR_` proposed). Write them into `source.levels` and use them everywhere; never
   invent a storey height.
4. **Block the plan.** One block per roofed mass, its width from the front elevation and its
   depth from the side elevation or the plan's dimension strings, eaves and ridge from the
   section; overhang and verges from the elevations. Scale from the 1:100 bar only when no
   figure is given, and say so in a `note`.
5. **Openings from the elevations.** Every window and door on each elevation by its cill and
   head and its run along the face; columns and arches as drawn. A door the works remove is
   an existing opening with `phase: "removed"`.
6. **The works from the proposed drawings.** The dormer's profile from the section (three
   points: cill, the knee, the top), its width from the rear elevation; rooflights from the
   Velux schedule — their count MUST equal the schedule's; new windows and brick infills from
   the elevations; the scaffold where the roof works are.
7. **The elements from the plans and the breakdown.** Walk the estimate's sections in build
   order — strip out, steels, floors, drainage, partitions, services, linings, joinery,
   sanitaryware, finishes — and place each as an element with the breakdown's cost code. Room
   layout comes from the plans' room names and dimension strings; when a position is inferred
   from a room's area rather than measured, say so in the element's `note`. Draw only what the
   drawings and the estimate say; never a room the plan does not name.
8. **The programme in client language.** Stages in the order the site would build them; each
   caption is what the prospect reads ("The tiles come off where the dormer will sit"), never
   trade jargon or a cost code.
9. **Check before storing.** Every part a programme step names must exist in the model; no
   element sits outside its block; every height sits on a level; the rooflight count matches
   the schedule; `source` names every sheet and its revision. Render it headless when a
   session can (the models folder's fixture shows the shape that renders).
10. **A person confirms, then store.** Show what the model covers — blocks, openings, the
    works, how many elements by trade, the stages — and the caveats, get the yes, then
    `set_house_model` (confirm-first) with `model` as the OBJECT and `source` as the sheets and
    revision. The previous definition is replaced whole.

## What to say about it

- It is **a massing model read off planning drawings, not a survey** — the caption says so
  and so do you. Good to a few centimetres where a dimension was given, to a brick or two
  where it was scaled. Never call it a survey, a BIM model or "accurate".
- The internal elements are a **first cut for the estimator to check** until they have been;
  the notes on them say what was inferred.
- The build-up is the prospect's view; the works view (the shell lifted off) is the trade
  view. Both read the same definition — never keep two.
