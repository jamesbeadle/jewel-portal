# Meeting: Bluebeam integration — Studio Projects canonical, take-off via CSV in v1, Markups API in phase 2

**Date:** 2026-05-25
**Attendees:** Nigel Reilly (project lead).

---

## Context

JBB already uses Bluebeam Revu for drawing review and take-off. Today the QS handles each drawing three times — saves the PDF from email to SharePoint, re-uploads it to Buildertrend, then opens it in Bluebeam — and exports the take-off to Excel before re-keying it into the BoQ. The aim was to design Bluebeam into JPMS such that the QS never re-handles a drawing and the take-off lands in JPMS without an Excel hop.

What the **Bluebeam Studio API** actually exposes (verified against the [Bluebeam developer pages](https://www.bluebeam.com/uk/product/integrations/)):

- **Studio Projects** — secure document storage with versioned files, accessible via the Studio Projects API.
- **Studio Sessions** — real-time multi-user PDF mark-up; the Sessions Roundtrip endpoint lets a host app launch a Session and invite collaborators.
- **Webhooks** — subscribe to Studio events (file added, file revised, markup status changed) instead of polling.
- **Markups API** — read and update markup status, including take-off measurement metadata embedded on each markup.

The Markups API does NOT expose "the BoQ" as a clean endpoint. Each take-off is a markup with a Subject, a measurement (length/area/count) and any custom columns the QS configured. To map cleanly into JPMS, take-offs need a JPMS cost-code custom column.

---

## Decisions

| # | Decision | Date |
|---|---|---|
| D1 | **Bluebeam Studio Projects is the canonical drawing store** from day one. Each JPMS project is linked to a Studio Project on creation. JPMS subscribes to Studio webhooks and reads new revisions via the Studio Projects API. The QS never re-uploads a drawing into JPMS — saving a new revision into the Studio Project IS the upload. | 2026-05-25 |
| D2 | **v1 take-off path: CSV import from Bluebeam Markups List export.** JPMS has a single "Import Bluebeam take-off" screen with column mapping → BoQ line items land directly (line, unit, qty, cost code). No standalone Excel. | 2026-05-25 |
| D3 | **Phase 2 take-off path: API-direct via the Bluebeam Markups API.** JPMS reads take-off markups directly from the linked Studio Project, lands BoQ lines automatically, refreshes on demand or on revision change. CSV remains as a fall-back. Phase 2 because the API path depends on the QS having consistently adopted the JPMS-published Bluebeam tool-set — a behaviour change worth piloting in v1 before building against. | 2026-05-25 |
| D4 | **JPMS publishes a Bluebeam tool-set** that includes a JPMS cost-code custom column on every take-off markup, so each take-off is tagged with its BoQ destination at source. Used by both the v1 CSV importer (the CSV inherits the column) and the phase-2 Markups API path. | 2026-05-25 |
| D5 | **Email-inbox fall-back retained** for architects who don't upload to Studio. JPMS ingests via the monitored inbox and re-publishes into the linked Studio Project so the Studio Project remains the canonical record regardless of how the architect issued the drawing. | 2026-05-25 |
| D6 | **Sessions Roundtrip deferred to phase 2.** Valuable for architects who don't already use Bluebeam day-to-day, but not needed for v1 — the team already does mark-up review inside Bluebeam Revu against the Studio Project. | 2026-05-25 |

---

## What this means for the QS

- Drawings appear in the JPMS drawing register without the QS uploading anything. They're sourced from the linked Bluebeam Studio Project; revisions arrive automatically via webhook.
- Click a drawing in the JPMS register → opens in Bluebeam Revu against the same Studio Project. No duplicate files, no version drift.
- Take-off in Bluebeam Revu, with the JPMS-published tool-set on hand so each measurement carries its cost code. Export the Markups List as CSV, import once in JPMS — BoQ line items land tagged to their cost codes.
- Phase 2: skip the CSV step entirely. JPMS reads the same take-off markups directly via the API.

---

## Artefacts updated

- [`/03-workflows/01-drawing-receipt-document-control.md`](../../03-workflows/01-drawing-receipt-document-control.md) — target flow rewritten: Bluebeam Studio Projects canonical, Studio Projects API + webhooks. Stories US-01-09 to US-01-12 added for the Studio integration and the inbox fall-back.
- [`/03-workflows/02-preconstruction-tender-boq.md`](../../03-workflows/02-preconstruction-tender-boq.md) — target flow rewritten with two take-off paths (v1 CSV, phase 2 API-direct). US-02-02 to US-02-04 cover the tool-set, CSV import and Markups API path. Open question on direct API vs export/import closed.
- [`/05-data-model/integrations.md`](../../05-data-model/integrations.md) — Bluebeam expanded to four lines (Studio Projects, Markups List CSV, Markups API, Sessions Roundtrip) with phase tagging. Phase-1 integration shortlist updated.
- [`/06-backlog/must-have-v1.md`](../../06-backlog/must-have-v1.md) — workflow 01 names Studio Projects as canonical; workflow 02 names the CSV path + tool-set.
- [`/06-backlog/phase-2.md`](../../06-backlog/phase-2.md) — new "Bluebeam — deeper integration" section with Markups API direct, Sessions Roundtrip, markup-status webhooks.
- [`/06-backlog/open-questions.md`](../../06-backlog/open-questions.md) — direct API question closed; new questions added on Studio Project provisioning and tool-set distribution.
- [`/01-personas/04-quantity-surveyor-estimator.md`](../../01-personas/04-quantity-surveyor-estimator.md) — goals and pain points refreshed; new "How JPMS changes this for the QS" section.

---

## Open questions surfaced

- [ ] One Studio Project per JPMS project, or a single Studio Project per Organisation (BB / PS / PFP) with folder-per-project inside?
- [ ] Studio Project provisioning — does JPMS create the Studio Project on project creation via API, or link to one the QS has already set up?
- [ ] Bluebeam tool-set distribution — published as a `.btx` file the QS imports manually, or auto-provisioned per machine when JPMS detects Bluebeam Revu?
- [ ] Architects without a Bluebeam licence — do we issue them a Studio Sessions invite (phase 2 Roundtrip), or stay on the email-inbox fall-back?
- [ ] Bluebeam licensing model for JBB — how many Studio API calls / month does the plan allow, and is the current licence sufficient for webhook-driven sync at portfolio scale?

---

## What we are NOT doing

- Treating JPMS as the canonical drawing store with a sync down to Bluebeam. The Studio Project is canonical; JPMS reads from it. Mirroring would re-introduce the version-drift problem the unified workflow exists to solve.
- Building a JPMS PDF mark-up tool. Mark-up stays in Bluebeam Revu — that's what it's for, and JBB already owns the licences.
- Shipping the Markups API path in v1. Without the QS consistently using the JPMS tool-set first, an API-direct importer would land messy data and be no better than the CSV path. We pilot the discipline in v1, then automate in phase 2.
