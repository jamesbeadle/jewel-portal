# Phase 1 vs Phase 2 — proposal for sign-off

**Status:** Draft proposal — for the business-owner walkthrough.
**Supersedes (on sign-off):** the implicit Phase 1 / Phase 2 split currently sitting in `must-have-v1.md`, `phase-2.md`, and the *Phase-1 integration shortlist* at the bottom of `05-data-model/integrations.md`. Those three files will be re-aligned once this proposal is signed off.

## The cut principle

**Phase 1** is the platform people on site and internal staff use directly. Data is entered into JPMS — by the PM, QS, Site Manager, OCC, subcontractor or architect — through the JPMS web app, mobile app or portal. JPMS runs the workflows, holds the records, produces the reports, and sends the notifications. The only external dependency in Phase 1 is **OAuth sign-in** (Google, Microsoft, email/password) because users have to log in.

**Phase 2** is the layer that *automates* the data entry — reading from third-party systems (Bluebeam, HMRC, monitored email inboxes) so JPMS picks up what staff currently type. Phase 2 also covers advanced automation features that go beyond data entry: OCR pre-fill, AI rate suggestion, completion-% prediction blending, what-if simulation.

The reason for being conservative on Phase 1 is that **every external integration is a configuration dependency on someone else's system** (the architect's Bluebeam setup, the QS's tool-set, HMRC's CIS service, an email tenancy). Each one is plausible to deliver and each one is a candidate for the *first* thing to slip the schedule. By moving them all to Phase 2 we protect Phase 1 delivery: the platform exists, people are using it, and Phase 2 work then layers automation on top of a running system rather than racing it.

Stories in Phase 1 do not depend on stories in Phase 2 — Phase 1 is internally complete. Phase 2 enhances Phase 1, it does not finish Phase 1.

---

## What this means for external integrations

The proposal is to move every integration except OAuth to Phase 2.

| System | Direction | Current README cut | **Proposed cut** | Phase 1 alternative |
|---|---|---|---|---|
| Google / Microsoft / email-password OAuth | → JPMS | P1 | **P1** (unchanged) | — |
| Bluebeam Studio Projects (API + webhooks) | ↔ JPMS | P1 | **P2** | PM/QS uploads drawing PDF into the JPMS drawing register directly. |
| Bluebeam Markups List CSV import | → JPMS | P1 | **P2** | QS enters BoQ line items directly in JPMS (`US-NEW-02`), or bulk-imports from a generic CSV/Excel template (`US-NEW-03`). |
| Bluebeam Markups API direct | → JPMS | P2 | **P2** (unchanged) | — |
| Bluebeam Sessions Roundtrip | ↔ JPMS | P2 | **P2** (unchanged) | — |
| Monitored email inboxes (drawings fall-back) | → JPMS | P1 | **P2** | PM/QS uploads drawing PDF on receipt (same as the Studio path above). |
| Monitored email inboxes (RFI replies) | → JPMS | P2 | **P2** (unchanged) | Architect uses the JPMS portal link (US-05-06). |
| HMRC CIS verification | ↔ JPMS | P1 | **P2** | OCC enters CIS status manually with the expiry date; JPMS chases renewal on schedule (US-03-14). |

If you accept this, the **Phase 1 integration shortlist** in `integrations.md` reduces to one line: *OAuth sign-in*.

---

## What this means at the user-story level

Of the **176 user stories** drafted across the 10 workflows (the README's headline of "170 stories" is stale — workflows 01 and 02 grew since it was last updated; flagged for a small `README.md` fix-up in Stage 2):

- **162 stories stay in Phase 1.** They run inside JPMS with no external-system dependency beyond OAuth.
- **14 stories move to Phase 2.** Listed below with rationale.
- **7 new Phase 1 stories** are proposed in `coverage-audit.md` to fill the gaps created by moving the Bluebeam-dependent and API-dependent stories out (manual drawing upload, direct BoQ entry, generic CSV import, rate staleness flag, TBT assignment, proactive client status update, drawing-issue audit record).

Each workflow is either *entirely Phase 1* or has a small Phase 2 carve-out. The next sections list only what is being proposed for Phase 2 — anything not mentioned is Phase 1 by default.

---

## Workflows entirely in Phase 1

These five workflows have no stories proposed for Phase 2. They are pure internal JPMS capability and ship as a unit.

- **Workflow 00 — Sales, Marketing & CRM** (14 stories).
- **Workflow 04 — H&S Site Mobilisation & Compliance** (16 stories). The HMRC CIS integration referenced inside the workflow is a Workflow 03 concern, not a 04 concern.
- **Workflow 06 — Site Delivery, Programme & Reporting** (12 stories). Subcontractor check-in is via QR scan (US-06-02) and manual override (US-06-03). Geofence-based check-in is *not currently a story* and is recorded as a candidate Phase 2 addition; no existing story moves.
- **Workflow 08 — Quality, Snags, Handover & Aftercare** (20 stories).
- **Workflow 09 — Portfolio Reporting & Analytics** (12 stories). The "recurring defects clustered by trade/subcontractor" story (US-09-06) is a simple group-by in Phase 1; advanced clustering is not in scope.

That is **74 of the 176 stories** in workflows entirely in Phase 1.

---

## Workflow 01 — Drawing Receipt & Document Control

The Phase 1 cut here is the most consequential: Bluebeam Studio Projects is the canonical drawing store in the current scope, but every story that depends on it moves to Phase 2. The Phase 1 drawing register is manual-upload-driven, with revision/version control as a JPMS capability.

**Stays in Phase 1** (delivers the drawing register with revision control, just via manual upload):
- US-01-02 (auto-extract revision from filename on ingest), US-01-03 (auto-supersede previous revision), US-01-04 (alert PM on ambiguous revisions), US-01-05 (PM override), US-01-06 (current revision on phone), US-01-07 (notify subcontractor on revision), US-01-08 (audit trail of viewers).

**Moves to Phase 2:**

| ID | Story summary | Rationale |
|---|---|---|
| US-01-01 | Architect uploads new revision into the project's Bluebeam Studio Project. | Bluebeam Studio Projects API dependency. P1 alternative: architect uploads PDF into the JPMS portal, or PM uploads on email receipt (covered by new story `US-NEW-01`). |
| US-01-09 | JPMS project linked to a Bluebeam Studio Project on creation. | Bluebeam Studio Projects API. |
| US-01-10 | JPMS subscribes to Bluebeam Studio webhooks per linked project. | Bluebeam webhooks. |
| US-01-11 | QS opens drawings in Bluebeam Revu directly from the JPMS drawing register. | Bluebeam deep link / single-sign-on to Revu. |
| US-01-12 | JPMS re-publishes fall-back inbox drawings into the linked Studio Project automatically. | Bluebeam Studio Projects + email-inbox ingest. |

**New Phase 1 story to add (from coverage audit):** `US-NEW-01` (manual drawing upload), `US-NEW-04` (record architect's drawing issue audit data on upload).

---

## Workflow 02 — Pre-Construction: Tender & BoQ

Phase 1 delivers the BoQ as a first-class JPMS record with hierarchical line items, rate library, M&E discipline tag and re-tender comparison. The take-off path moves to Phase 2 (it's the most-Bluebeam-dependent workflow in the system).

**Stays in Phase 1:**
- US-02-05 (rate library inside JPMS), US-02-07 (discipline tag including M&E), US-02-08 (re-tender comparison), US-02-09 (walk-round capture on mobile), US-02-10 (export finalised BoQ for external use), US-02-11 (Director signs off final tender).

**Moves to Phase 2:**

| ID | Story summary | Rationale |
|---|---|---|
| US-02-01 | Create project record from incoming tender with drawings already in the linked Bluebeam Studio Project. | Bluebeam Studio Projects dependency. P1 alternative: project record created and drawings uploaded manually into the JPMS drawing register. |
| US-02-02 | Take-off in Bluebeam Revu using JPMS-published tool-set with cost-code column. | Bluebeam tool-set distribution + Revu. |
| US-02-03 | Export Bluebeam Markups List as CSV and import into JPMS. | Bluebeam-specific CSV format and Bluebeam tool-set adoption. |
| US-02-04 | JPMS reads take-off markups from Studio Project via Markups API. | Bluebeam Markups API. (Already P2 in `phase-2.md`.) |
| US-02-06 | JPMS suggests updated rates when stale (AI-assisted). | Advanced AI feature. P1 alternative: JPMS flags rates that haven't been priced in N days (new story `US-NEW-06`). |

**New Phase 1 stories to add (from coverage audit):** `US-NEW-02` (direct BoQ entry), `US-NEW-03` (bulk import BoQ from generic CSV/Excel), `US-NEW-06` (rate staleness flag).

---

## Workflow 03 — Subcontractor Procurement & Onboarding

Almost entirely Phase 1. Two stories move out because they depend on external automation (HMRC CIS API; OCR on uploaded compliance documents).

**Stays in Phase 1:**
- US-03-01 to US-03-15, US-03-18 to US-03-22 (bid package builder, subcontractor portal, comparison, award, compliance gate, work-order generation, expiry reminders, RAMS drafting/sending, CIS *visibility*).

**Moves to Phase 2:**

| ID | Story summary | Rationale |
|---|---|---|
| US-03-16 | Drag-and-drop upload of compliance documents with OCR pre-filling the expiry date. | OCR is an advanced automation feature. P1 alternative: drag-and-drop upload with manual entry of the expiry date — the chase-and-remind logic (US-03-14) works the same way against a manually entered date. |
| US-03-17 | One-click CIS refresh that calls HMRC. | HMRC CIS API. P1 alternative: OCC enters CIS status manually; JPMS chases renewal (US-03-14) and surfaces it on the subcontractor record (US-03-22). |

---

## Workflow 05 — RFIs, Submittals, Variations & Delays

All 12 stories stay in Phase 1.

The integrations file already places the *email-inbox channel for architect RFI replies* in Phase 2, and the existing US-05-06 already specifies the Phase 1 path (architect uses the JPMS portal link). No story moves.

---

## Workflow 07 — Valuations, Cashflow & Forecasting

Almost entirely Phase 1 — this is where the platform earns its keep. Three stories propose to move to Phase 2 because they describe advanced features (predictive blending, stress-test simulation) rather than the core data-driven output. The base PVR, CVR and cashflow numbers all stay in Phase 1.

**Stays in Phase 1:** all PVR stories (US-07-01 to US-07-10), all timesheets / cost-code stories (US-07-12 to US-07-22), all CVR stories (US-07-23 to US-07-35) — this is the CVR rebuild that replaces Planyard, and it has to land in Phase 1 to deliver on the headline JPMS promise. Cashflow stories US-07-36, US-07-37, US-07-38, US-07-39, US-07-40, US-07-42, US-07-44, US-07-45, US-07-46.

**Moves to Phase 2:**

| ID | Story summary | Rationale |
|---|---|---|
| US-07-41 | Stress-test slider — *"what if completion % slips by N%?"* — re-runs the projection without changing source data. | Advanced what-if modelling feature. P1 cashflow shows the current forecast and the items-needing-attention queue (US-07-39); the slider is an enhancement that doesn't change a number until P2. |
| US-07-43 | Completion-% prediction blending site-reported % with timesheet-vs-budget burn rate. | Advanced predictive model. P1: completion-% is the site-reported % (workflow 06). Burn-rate blending is a model that needs data history before it's accurate — fits Phase 2. |

**The CVR stories stay in Phase 1 in full.** Forecast component breakdown, QS Accruals, Prelim Forecast, EOT register, per-package variation margin, Movement column, snapshots — these are the *whole point* of replacing Planyard and the Excel CVR. If any of these slip to Phase 2, the case for Phase 1 weakens significantly. The CVR is the Phase 1 commercial backbone.

---

## Summary of stories moving to Phase 2

14 stories in total, across four workflows:

| Workflow | Story IDs moving to P2 | Count |
|---|---|---|
| 01 Drawing | US-01-01, US-01-09, US-01-10, US-01-11, US-01-12 | 5 |
| 02 Tender & BoQ | US-02-01, US-02-02, US-02-03, US-02-04, US-02-06 | 5 |
| 03 Procurement & Onboarding | US-03-16, US-03-17 | 2 |
| 07 Valuations / Cashflow | US-07-41, US-07-43 | 2 |
| 05 RFIs, Submittals, Variations, Delays | — | 0 |
| **Total** | | **14** |

Stories that remain in Phase 1: **169 = 162 from the original 176 + 7 new Phase 1 stories from the audit (`US-NEW-01` to `US-NEW-07`).**

---

## What Phase 2 looks like after this proposal

Phase 2 work bundles into three coherent tracks rather than scattered single items:

1. **Bluebeam track** — Studio Projects sync (workflow 01), tool-set adoption + Markups CSV import (workflow 02), Markups API direct (workflow 02), Sessions Roundtrip (workflows 01 + 05). Ten stories together. This is a single conversation with the QS team and a single integration build.
2. **External system integration track** — HMRC CIS API (workflow 03), email-inbox ingest for drawings and RFI replies (workflows 01 + 05), OCR on compliance documents (workflow 03).
3. **Advanced cashflow track** — stress-test simulation (US-07-41), completion-% prediction blending (US-07-43). Possibly more once Phase 1 cashflow data accumulates.

Plus the existing Phase 2 items already in `phase-2.md`: long-lead procurement, marketing automation, specs intelligence, multi-tenant.

The shape that falls out is: **Phase 1 = the working platform; Phase 2 = the automation track + the procurement-scope expansion.** That reads cleanly to the business owner because each track has a recognisable purpose.

---

## What I'm asking you to confirm

Three things, in order:

1. **The cut principle** — Phase 1 = direct entry into JPMS, Phase 2 = automation and external-system ingestion. Confirms the conservative bias.
2. **The integration cuts** in the table above — particularly that Bluebeam Studio Projects, Bluebeam Markups CSV, the email-inbox fall-back, and HMRC CIS all move to Phase 2.
3. **The 14 stories listed for Phase 2** — confirm each (or push back specific ones).

Once you confirm, Stage 2 of the refactor folds these tags into every workflow file (each user story carries `[P1]` or `[P2]`), updates `must-have-v1.md` and `phase-2.md` to match, adds the seven new Phase 1 stories to their workflow files, and builds the role-then-workflow master walkthrough document for the business-owner session.
