# Workflow 02 — Pre-Construction: Tender & BoQ

**Group:** Project lifecycle
**Purpose:** Convert tender drawings and scope of works into a priced Bill of Quantities ready for the bid process.
**Trigger:** New tender opportunity received; or existing project re-tender required.
**Frequency:** Per tender — typically several active per month.
**Owner (target):** Project & Commercial Lead (review and judgement); JPMS for data assembly.
**Current monthly hours:** ~50 h/month.
**Status:** Draft
**Last reviewed:** 2026-09-16 (take-off route corrected: read from the drawing, not Bluebeam)

---

## Current state

1. Estimator reviews tender drawings in Bluebeam.
2. Estimator does take-off in Bluebeam, exports quantities to Excel.
3. Estimator researches updated rates manually (web, supplier contacts).
4. Estimator reviews M&E drawings separately to create an M&E BoQ.
5. Everything is assembled in a standalone Excel BoQ that lives outside any project system.
6. Walk-rounds and on-site information are captured in notebooks / notes apps.

---

## Target flow (post-automation)

1. New project record created in JPMS — the tender drawings already sit in its drawing register (see workflow 01), filed from the projects mailbox or uploaded, and every revision is transcribed as it lands.
2. **Take-off is read from the drawing itself** (decided 2026-09-07). The portal extracts each PDF's own vector geometry and positioned text — title block, revision table, the sheet's proven scale, every figured dimension, callout and closed shape — and stores them as rows the assistant queries and totals (`query_document_data`: by kind, page, text, axis, value or area range, rectangle, near a point). Measurement is exact at the proven scale and depends on no outside connection; a scanned sheet with no vector layer is the one that still needs a person.
3. **Quantities become BoQ lines from those rows** — lengths, areas and counts against their spec notes, tagged to a cost code as they are taken (the story on the backlog; today the assistant reads the rows and the QS confirms the figures). No standalone Excel.
4. **Revu markups are an optional import, not the route.** Bluebeam's Markups API returns only markups a person has drawn in Revu and nothing on a drawing nobody has marked up, so it is not a take-off engine. Where the QS has genuinely measured in Revu, those markups are read into the same extraction alongside the geometry; the extraction never waits on the Bluebeam connection to run.
5. Rate library held in JPMS, with last-used rates per trade and supplier; AI suggests updated rates.
6. M&E section produced through the same workflow with discipline tagging.
7. Walk-round notes/photos captured against the project on mobile.
8. Final BoQ exported from JPMS — the master record stays in JPMS, not Excel.

---

## JPMS functionality required

- Project records (pre-construction phase).
- BoQ module with hierarchical line items, units, rates.
- Rate library with version history and supplier links.
- **Drawing transcription** (built) — every revision's geometry and text read as it lands; rows per dimension, callout and shape; the summary and the row query over the connector.
- **Quantities from the transcription** (backlog) — lengths, areas and counts taken off the rows against their spec notes and landed as BoQ lines with a cost code.
- **Revu markups import** (optional, built) — a person's own Revu measurements read into the same extraction when they exist; never required.
- Mobile walk-round capture (notes, photos, voice notes).
- Re-tender comparison view (last priced vs current).

---

## Integrations & adjacent systems

- **The JPMS drawing register** (drawings sourced here; see workflow 01).
- **Bluebeam Markups API** (optional — a person's Revu markups only; not the take-off route).
- **Supplier rate data** (manual today; AI-assisted in target flow).
- **JPMS rate library** (the canonical source after rollout).

---

## User stories

| ID | Role | Story | Status |
|---|---|---|---|
| US-02-01 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to create a new project record from an incoming tender with the architect's drawings already in the linked Bluebeam Studio Project, so that everything else hangs off one project from day one. | Drafted |
| US-02-02 | P04 Quantity Surveyor | As a QS, I want to do take-off in Bluebeam Revu using a JPMS-published tool-set with a cost-code column on every markup, so that each take-off is tagged with its JPMS destination at source. | Drafted |
| US-02-03 | P04 Quantity Surveyor | As a QS, I want to export the Bluebeam Markups List as CSV and import it into JPMS through a single import screen with column mapping, so that take-off lands as BoQ line items without me re-keying into Excel. (v1 path.) | Drafted |
| US-02-04 | JPMS (system) | As JPMS, I want to read take-off markups directly from the linked Bluebeam Studio Project via the Markups API and land them as BoQ line items, refreshing when the QS adds new markups or revises a drawing. (Phase 2 path; CSV remains as a fall-back.) | Drafted |
| US-02-05 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want a rate library inside JPMS with last-used rates per trade and supplier, so that I'm not researching rates from scratch on every tender. | Drafted |
| US-02-06 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want JPMS to suggest updated rates against my rate library when it spots stale values, so that the BoQ reflects current pricing without me hunting. | Drafted |
| US-02-07 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to tag BoQ lines by discipline (including M&E), so that M&E pricing can be assembled through the same flow rather than a separate spreadsheet. | Drafted |
| US-02-08 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want a re-tender comparison view showing last-priced vs current rates per line, so that re-tendering a previous project takes hours, not days. | Drafted |
| US-02-09 | P05 Site Team | As a site manager doing a pre-construction walk-round, I want to capture notes, photos and voice notes against the project on mobile, so that the information is tied to the project record from day one. | Drafted |
| US-02-10 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to export the finalised BoQ from JPMS for external use, so that I can share it where needed while JPMS remains the master record. | Drafted |
| US-02-11 | P01 Directors / MD | As a Director, I want to sign off the final tender before it's issued back to the architect, so that high-value commitments don't go out without approval. | Drafted |

Covers spreadsheet rows 13 (Bluebeam take-off), 14 (research rates), 20 (re-tender), 21 (Bluebeam-formatted quants), 23 (M&E BoQ).

---

## Acceptance criteria — "done looks like"

- BoQ exists as a JPMS record, not a standalone Excel file.
- Re-tender of a previous project takes hours, not days.
- Walk-round notes are tied to the project record from day one.

---

## Entities touched

`Project` · `Tender` · `BoQ` · `BoQ Line Item` · `Rate` · `Drawing`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Project & Commercial Lead | Owner — review and judgement on rates |
| Quantity Surveyor (where retained) | Contributor — confirms the quantities read off the drawings, and prices |
| Site Team | Contributor — walk-round capture on mobile |
| Architect (external) | Source — drawings and specs |

---

## Open questions

- [x] Bluebeam — is direct API integration available, or do we ship via export/import file? **Decided 2026-05-25:** Both, in two phases (CSV, then the Markups API). See [`/00-business-context/meetings/2026-05-25-bluebeam-integration.md`](../00-business-context/meetings/2026-05-25-bluebeam-integration.md). **Superseded 2026-09-07:** neither is the take-off route. The Markups API only returns markups a person drew in Revu and nothing on an unmarked drawing (an architect's Revit export read on 9 Sep carried no annotation layer at all, and a 7,000-character vector text layer); measurement now comes from the drawing's own geometry and text, and Revu markups are an optional import. The CSV importer and the JPMS tool-set are withdrawn. Still to prove: whether the sessions endpoint returns markups already saved in the file before upload or only ones drawn in the live session — task 322b43ce settles it.
- [ ] ~~Bluebeam tool-set distribution~~ — withdrawn with the tool-set (2026-09-07).
- [ ] Rate library — supplier list as a JPMS entity, or just a lookup against the supplier directory?
- [ ] M&E discipline tag — single field or full discipline hierarchy?
- [ ] AI rate suggestion — confidence threshold to auto-apply vs flag for review?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
