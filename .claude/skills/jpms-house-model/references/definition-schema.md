# The house-model definition — field by field

The JSON `set_house_model` stores and `js/house-model/` builds. Metres throughout; `x` along
the front from the party wall, `z` from the front wall to the garden, `y` heights above the
ground-floor finished floor. The worked example is 16 Ravens Dene (EST-0001), kept in the
repository as `jpms/wwwroot/js/house-model/models/ravens-dene-16.json`; the fragments below
are taken from it.

## Top level

| key | what it is |
|---|---|
| `name` | The property — the caption and the scene's name. Required. |
| `source` | `sheets[]` (each sheet with its number, title, revision and date), `revision`, `readOn`, `levels` (the section's level lines by name) and `caveat` (what the caption says). |
| `centre` | `[x, y, z]` the camera views are set out from — roughly the middle of the house at first-floor height. |
| `blocks` | The roofed masses. Required, at least one. |
| `faces` | The elevations openings sit on, by name. |
| `openings`, `infills`, `rooflights`, `dormer`, `downpipes`, `context` | The house parts, below. |
| `phases`, `trades`, `elements` | The internal works, below. |
| `programme` | The build-up, below. |

## Blocks and faces

A block is a brick box with a gable roof whose ridge runs along `x`:

```json
{"name": "house", "x": 0, "z": 0, "width": 5.7, "depth": 9.9, "eaves": 5, "roof": {"ridge": 9.27, "overhang": 0.3, "vergeLeft": 0, "vergeRight": 0.25}}
```

`x`, `z` the front-left corner in plan; `width` along the front, `depth` towards the garden;
`eaves` the height the walls stop at; `roof.ridge` the ridge height, `overhang` past the eaves,
`vergeLeft` / `vergeRight` past the gables (0 against a party wall). A face names an elevation
for openings to sit on — `axis` the axis the face runs across, `at` its position, `outward`
which way it looks:

```json
{"front": {"axis": "z", "at": 0, "outward": -1}, "rear": {"axis": "z", "at": 9.9, "outward": 1}, "right": {"axis": "x", "at": 5.7, "outward": 1}, "garageFront": {"axis": "z", "at": 4.3, "outward": -1}, "garageRear": {"axis": "z", "at": 10.9, "outward": 1}}
```

## Openings, infills, rooflights, dormer, downpipes

An opening sits on a face by `u` (its run along the face) and `y` (cill to head):

```json
{"face": "front", "kind": "window", "u": [1.1, 2.47], "y": [0.97, 2.14], "columns": 3, "arched": true}
{"name": "garage door", "face": "garageFront", "kind": "garageDoor", "u": [6, 8.4], "y": [0, 2.1], "phase": "removed"}
```

`kind` is `window`, `door`, `slidingDoors` or `garageDoor`; `columns` the number of panes
across; `arched` for a brick arch over it; `canopy` for a porch canopy; `phase` `existing`
(the default), `proposed` or `removed`. A named opening can be moved by the programme.

An infill is new brickwork closing an opening; a rooflight sits on a block's `front` or
`rear` slope at `x` along the eaves, `sillHeight` up the slope; the dormer's `profile` is
three points in the z–y plane on the rear slope (cill, knee, top) and `x` its run:

```json
{"name": "garage infill", "face": "garageFront", "u": [6, 8.4], "y": [0, 2.1], "phase": "proposed"}
{"reference": "R11", "block": "house", "side": "front", "x": 1.38, "sillHeight": 7.03, "width": 0.78, "length": 1.18, "phase": "proposed"}
{"x": [1.46, 4.34], "profile": [{"z": 8.87, "y": 5.89}, {"z": 8.37, "y": 7.2}, {"z": 6.6, "y": 7.85}], "columns": 3, "phase": "proposed"}
```

Downpipes are `{ x, z, top }`. `context` holds `paving[]` (`x` and `z` ranges), `fences[]`
(`from` / `to` in plan) and `neighbours[]` (blocks drawn quietly, no openings).

## Phases, trades and elements — the internal works

`phases` is the project's ordered list; the first is what stands today. `trades` gives each
trade its colour in the works view:

```json
[{"key": "00", "name": "Existing"}, {"key": "01", "name": "Strip out"}, {"key": "02", "name": "Structure"}] …
[{"key": "demolition", "name": "Demolition", "colour": "#c0392b"}, {"key": "groundworks", "name": "Groundworks", "colour": "#7f6a4f"}] …
```

An element is a footprint polygon (`[x, z]` pairs, any number of them) extruded from `base`
to `top`:

```json
{"id": "wall-living-kitchen", "name": "Wall between living / dining and kitchen — removed", "trade": "demolition", "footprint": [[3.4, 6], [3.5, 6], [3.5, 9.6], [3.4, 9.6]], "base": 0, "top": 2.31, "phaseIn": "00", "phaseOut": "01", "costCode": "ENABLE-DEM", "note": "Kitchen taken as the rear right room (10 m²); the opening spans its length."}
{"id": "stud-bed6-landing", "name": "Loft — Bedroom 6 / landing partition", "trade": "partitions", "footprint": [[3.5, 1.5], [3.6, 1.5], [3.6, 5.9], [3.5, 5.9]], "base": 5.33, "top": 7.63, "phaseIn": "04", "phaseOut": null, "costCode": "CARP-1FX", "note": "Bedroom 6 is the front room lit by R9–R11."}
```

`phaseIn` is the phase it joins in; `phaseOut` the phase it leaves in, or `null` when it
stays. Joining in the first phase and leaving means the works remove it (shown in red,
demolition's colour); joining later and staying is proposed work; joining and leaving later
is temporary work. `costCode` is the estimate line it is priced on (a Code from
`list_cost_codes`); `variationRef` (`V72`) when a variation drove it; `note` says what was
inferred rather than measured. `id` is what a programme step names.

## The programme

```json
{"scaffold": {"standardsAt": [-0.1, 1.85, 3.8, 5.75], "rowsAt": [10.35, 11.35], "lifts": 3, "phase": "construction"}, "roofStrip": {"name": "roof strip", "block": "house", "side": "rear", "x": [1.31, 4.49], "alongSlope": [1.15, 4.6], "phase": "construction"}}
```

`scaffold` — `standardsAt` the x positions of the standards, `rowsAt` the two z rows, `lifts`
how many; `roofStrip` the patch of slope stripped for the dormer. Then `stages[]`, each with
`title`, `detail` (the caption), `view` (a camera view: overview, garden, road, side, above,
dormer, frontRoof, garageFront, garageGarden), `hold` (ms to pause after its last move) and
`steps[]` — `part` (a part's name or an element's id), `kind` (grow, shrink, drop, lift,
appear, vanish, fade), `at` (ms from the stage start), `duration`, and for drop `from` /
lift `by` an offset in metres:

```json
{"title": "The garage comes out of use", "view": "garageFront", "hold": 2200, "detail": "The up-and-over door comes out and the opening is bricked up, with a window for the new utility room.", "steps": [{"part": "garage door", "kind": "lift", "by": [0, 2.3, 0], "duration": 1800}, {"part": "garage infill", "kind": "grow", "at": 1900, "duration": 2300}, {"part": "utility window", "kind": "drop", "from": [0, 0, -0.6], "at": 4400, "duration": 1000}]}
```

Parts the builders name: `<block> walls`, `<block> gable`, `<block> roof`, an opening's
`name` (else `<kind> on <face>`), an infill's `name`, `rooflight <reference>`, `glazed
dormer`, `roof strip`, `scaffold standards`, `scaffold lift N`, and every element's `id`.
