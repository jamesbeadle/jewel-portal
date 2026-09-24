---
name: jpms-contractors-report
description: "The weekly Contractor's Report (the project report sent to the client's side) — load before drafting, composing, reviewing or writing up any Contractor's Report or weekly project report, whether through create_contractors_report / get_contractors_report or as a document written by hand. Encodes the Friday-to-Thursday period, match-file-draft for photographs, the wording gate, and what the report never names: no work-order numbers and no subcontractor target completion dates (Nigel, 20/09/2026)."
---

# JPMS — The weekly Contractor's Report

The report goes to the client's side (architect, client, PLG). Everything in it is written
for that reader, not for Jewel's own commercial team.

## The run

1. **The period is Friday to Thursday.** Section 1 reads the selected progress updates under
   Friday (weekend folded in), then Monday–Thursday.
2. **Photographs: match, file, draft.** Hash every image in the week's folder →
   `match_site_photos` once with all the hashes → `file_site_photos` per day onto that day's
   progress update (`create_progress_update` for a day with none). A hash the pool does not
   hold is a file nobody has dropped yet: name it and ask. Never paste or re-encode an image.
3. `create_contractors_report`, then `get_contractors_report` to read the composed document
   and its findings. Word and PDF are downloaded from the page by a person, who sends it.

## What the report never says

- **No work-order numbers and no target completion dates** (Nigel, 20/09/2026, on the By
  France report: "don't mention WO or target subbie completion"). Section 8 names the
  subcontractor and the work they did in the week — nothing else about the order. The
  `reference` and `targetCompletion` that `get_contractors_report` returns for each work order
  are there for you to match site notes to the right company; they are never written into
  the report, in the table or in the prose ("WO-0049 tiling installation (target completion
  9 October 2026)" is wrong; "Wall tiling to the first and second floors (Friday)" is right).
  The same goes for anything written by hand as the weekly project report.
- **The wording gate.** remedial, remedial works, making good, rectify, rectification,
  snagging, defects, rework — whole words, any case. A hit is a finding naming section and
  line, and the Word/PDF builds are refused while any stands. Fix the line on its record
  (`update_contractors_report` for entered text, `update_progress_update` for a site note);
  never reword silently, and write new text so it never trips the gate.
