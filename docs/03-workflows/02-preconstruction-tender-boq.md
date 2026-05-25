# Workflow 02 — Pre-Construction: Tender & BoQ

**Group:** Project lifecycle
**Purpose:** Convert tender drawings and scope of works into a priced Bill of Quantities ready for the bid process.
**Trigger:** New tender opportunity received; or existing project re-tender required.
**Frequency:** Per tender — typically several active per month.
**Owner (target):** Project & Commercial Lead (review and judgement); JPMS for data assembly.
**Current monthly hours:** ~50 h/month.
**Status:** Draft
**Last reviewed:** —

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

1. New project record created in JPMS with the linked Bluebeam Studio Project (see workflow 01) — tender drawings already live there.
2. QS does take-off in Bluebeam Revu against the drawings in the Studio Project, using the JPMS-published Bluebeam tool-set (each take-off markup carries a Subject and a JPMS cost-code custom column).
3. **v1 take-off path:** QS exports the Markups List from Bluebeam Revu as CSV. JPMS has a single "Import Bluebeam take-off" screen with column mapping → the import lands BoQ line items (line, unit, qty, cost code) directly. No standalone Excel.
4. **Phase 2 take-off path:** JPMS reads markups from the project's Studio Project via the **Bluebeam Markups API**, lands BoQ lines automatically, refreshes on demand or on revision change. CSV remains available as a fall-back.
5. Rate library held in JPMS, with last-used rates per trade and supplier; AI suggests updated rates.
6. M&E section produced through the same workflow with discipline tagging.
7. Walk-round notes/photos captured against the project on mobile.
8. Final BoQ exported from JPMS — the master record stays in JPMS, not Excel.

---

## JPMS functionality required

- Project records (pre-construction phase).
- BoQ module with hierarchical line items, units, rates.
- Rate library with version history and supplier links.
- **Bluebeam tool-set / custom column profile** — JPMS publishes a Bluebeam tool-set that includes a JPMS cost-code column on every take-off markup, so each take-off is tagged at source.
- **Bluebeam take-off CSV importer (v1)** — single import screen, column mapping to BoQ + cost code, validation, preview before commit.
- **Bluebeam Markups API integration (phase 2)** — read markups directly from the linked Studio Project, refresh on demand or on revision change.
- Mobile walk-round capture (notes, photos, voice notes).
- Re-tender comparison view (last priced vs current).

---

## Integrations & adjacent systems

- **Bluebeam Studio Projects** (drawings sourced here; see workflow 01).
- **Bluebeam Markups List CSV export** (v1 take-off path).
- **Bluebeam Markups API** (phase 2 take-off path).
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
| Quantity Surveyor (where retained) | Contributor — take-off and pricing |
| Site Team | Contributor — walk-round capture on mobile |
| Architect (external) | Source — drawings and specs |

---

## Open questions

- [x] Bluebeam — is direct API integration available, or do we ship via export/import file? **Decided 2026-05-25:** Both, in two phases. v1 ships CSV import from Bluebeam's Markups List export. Phase 2 adds API-direct take-off via the Bluebeam Markups API once the QS has adopted the JPMS-published tool-set consistently. See [`/00-business-context/meetings/2026-05-25-bluebeam-integration.md`](../00-business-context/meetings/2026-05-25-bluebeam-integration.md).
- [ ] Bluebeam tool-set distribution — published as a `.btx` file the QS imports manually, or auto-provisioned per machine when JPMS detects Bluebeam Revu?
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
