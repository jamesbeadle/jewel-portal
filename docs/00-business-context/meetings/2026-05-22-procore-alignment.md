# Meeting: Procore-style alignment pass — restructure into the 11-role / 10-lifecycle model

**Date:** 2026-05-22
**Attendees:** Nigel Reilly (project lead), JBB business owner (async review by email)

---

## Context

The business owner reviewed the repo, brought in observations from Procore's connected-platform model, and gave structured feedback in two options. The agreed decision is **Option 2 — full Procore-style restructure** because it reflects how JBB actually operates and the cost of restructuring now is much less than the cost of carrying an under-modelled system forward.

---

## Decisions

| # | Decision | Date |
|---|---|---|
| D1 | The repo restructures into a Procore-style operating model. New top-level folders under `/docs/`: `00-business-context`, `01-personas`, `02-lifecycle`, `03-workflows`, `04-user-journeys`, `05-data-model`, `06-backlog`. | 2026-05-22 |
| D2 | The persona register grows from 7 to **11 roles**: P01 Director / MD, P02 Finance Director, P03 Project Manager, P04 QS / Estimator, P05 Site Manager, P06 H&SO, P07 Office & Compliance Coordinator, P08 Architect / Designer / Consultant, P09 Client / Homeowner, P10 Subcontractor, P11 Foreman / Site Team. The previous P03 Project & Commercial Lead splits into P03 PM and P04 QS. P05 Site Team splits into P05 Site Manager and P11 Foreman / Site Team. New: P06 H&SO and P09 Client / Homeowner. | 2026-05-22 |
| D3 | The workflow set grows from 11 to **10 lifecycle stages numbered 00–09**. Three new workflows: **00 Sales, Marketing & CRM** (lifecycle starts before drawing receipt); **04 H&S Site Mobilisation & Compliance** (first-class H&S engine, owned by H&SO with Site Manager confirmation); **09 Portfolio Reporting & Analytics** (Director / FD cross-project view). The previous 11 workflows merge / split into the new structure. | 2026-05-22 |
| D4 | The **Inspections engine** is a first-class capability owned by workflow 04 and used by 06 (quality observations) and 08 (snag inspections). Likewise the **Observations / Incidents / Corrective Actions engine** is owned by 04 and used cross-cutting. **Submittals & Approvals** are part of workflow 05 (RFIs / Submittals / Variations / Delays). | 2026-05-22 |
| D5 | The 157 user stories carry forward and continue with their `US-NN-MM` IDs; stories from old workflows have been renumbered to match the new workflow IDs. New stories for workflows 00, 04 and 09 surface in this pass. | 2026-05-22 |
| D6 | The **data model** moves into `/05-data-model/` and is split: `entities.md`, `permissions-matrix.md`, `status-models.md`, `approval-flows.md`, `integrations.md`. | 2026-05-22 |
| D7 | The **backlog** moves into `/06-backlog/`: `must-have-v1.md`, `phase-2.md`, `open-questions.md`. | 2026-05-22 |

---

## What the client called out specifically (and is now in)

- Site Manager and H&SO as **distinct personas** rather than absorbed into Site Team / OCC.
- A **first-class H&S workflow** with mobilisation gate, inspections, audits, incidents, near-misses, corrective actions, toolbox talks, permits-to-work.
- **Sales / Marketing / CRM** as an upstream lifecycle stage before drawing receipt, with a clean won → project-shell handoff.
- **Document control** beyond receipt — revision history, distribution / acknowledgment, linked RFIs / variations.
- **Submittals & approvals** before installation as a distinct workflow capability.
- **Snag / punch management** as its own engine within 08.
- **Correspondence / instruction log** as the master record of project communications.
- **Portfolio analytics** for Director and FD — cross-project leading indicators.

## What is explicitly out / deferred to Phase 2

See [`/06-backlog/phase-2.md`](../../06-backlog/phase-2.md). Notably: long-lead material procurement tracking, BIM coordination, advanced specs intelligence, multi-tenant beyond JBB.

---

## What does not change

- The technical approach (Blazor WASM + ASP.NET Core + Azure SQL + OAuth).
- The two key outputs JPMS produces (Programme Valuation Report, cashflow forecast).
- The boundary against accountancy (Xero / Brightpay / Dext / Chaser HQ remain downstream).
- The customer-facing root README shape — top-down narrative for the business owner, technical detail further down.

---

## Artefacts updated

- Created `/00-business-context/` with overview, operating model, delivery principles, commercial model, glossary, and meeting notes archive.
- Created `/01-personas/` with 11 individual persona files.
- Created `/02-lifecycle/` with 10 stage descriptions plus an index.
- Created `/03-workflows/` with 10 detailed workflow files: 7 migrated from the old structure, 3 brand new (00 CRM, 04 H&S, 09 Portfolio Analytics).
- Moved 5 user journeys into `/04-user-journeys/` with renumbered IDs.
- Created `/05-data-model/` with entities, permissions matrix, status models, approval flows, integrations.
- Created `/06-backlog/` with must-have-v1, phase-2, open-questions.
- Deleted the old `/docs/workflows/`, `/docs/user-journeys/`, `/docs/requirements/`, `/docs/data-models/`, `/docs/meetings/`, `/docs/ui-components/` folders.
- Updated root README to reflect the new structure and roadmap.
