# Permission Matrix (Role × Workflow)

Coarse-grained matrix of which role is responsible for what across the twenty-one workflows. This is the at-a-glance reference. Per-step CRUD permissions live in each workflow file (and, once written, in each user-journey file).

**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

**Status:** Draft — refined per workflow as the deep-dive sessions happen.

---

## Legend

- **O** — Owner. Accountable for the workflow end-to-end.
- **A** — Approver. Sign-off authority at a defined step.
- **C** — Contributor. Materially supplies data or carries out steps.
- **R** — Read access only.
- **—** — No access / not involved.

When two letters apply, list both (e.g. `O / A` for "owns and signs off"; `C (M)` for "contributor via mobile only").

---

## Roles

| ID | Role | Type |
|---|---|---|
| P01 | Architect | External |
| P02 | Quantity Surveyor (QS) | Internal / external |
| P03 | Subcontractor | External |
| P06 | Project & Commercial Lead | Internal |
| P07 | Office & Compliance Coordinator | Internal |
| P08 | Site Team | Internal |
| P09 | Brand & Content | Internal |
| P10 | Finance Director (FD) | Internal exec |
| P11 | Directors / MD | Internal exec |
| P12 | Outsourced IT Helpdesk | External partner |

> P04 Accountant folds into P10. P05 MD folds into P11. See [`personas.md`](personas.md).

---

## Matrix

| # | Workflow | P01 Architect | P02 QS | P03 Subbie | P06 PCL | P07 OCC | P08 Site | P09 Brand | P10 FD | P11 Dir | P12 IT |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 01 | Drawing Receipt | C (source) | R | R | A | — | R (M) | — | — | R | — |
| 02 | Tender & BoQ | C (source) | C | — | **O** | — | C (M) | — | R | A | — |
| 03 | Subbie Procurement | — | R | C (source) | **O / A** | C | — | — | R | A (high value) | — |
| 04 | Variations / RFIs / NoDs | A | C | C (source) | **O** | — | C | — | R | A (high value) | — |
| 05 | Programme & Valuations | R (receives) | R | — | **O / A** | — | C | — | R | A | — |
| 06 | Site Reporting | R (live dashboard) | R | C | A | — | **O** | — | — | R | — |
| 07 | Close-Out & Defects | R | — | C | **O / A** | — | C | — | A (retention) | R | — |
| 08 | Subbie Compliance | — | — | C (self-service) | R | **O** | — | — | R (gates pay) | R | — |
| 09 | Accounts Payable | — | — | C (invoice) | R | R | — | — | **O / A** | A (above threshold) | — |
| 10 | Accounts Receivable | R (recipient) | — | — | C (trigger) | — | — | — | **O / A** | R | — |
| 11 | Cashflow & Mgmt Reporting | — | — | — | R (project slice) | — | — | — | **O** | A | — |
| 12 | Payroll | — | — | — | — | C (starter/leaver) | C (timesheets) | — | **O / A** | A | — |
| 13 | Accounts Inbox Triage | — | — | — | — | C (some queries) | — | — | **O** | — | — |
| 14 | Client & Reactive Comms | C (source) | — | — | A (project) | **O** | — | — | — | R | — |
| 15 | Materials & Deliveries | — | — | — | A (threshold) | **O** | C (req + GRN) | — | R | — | — |
| 16 | HR, Onboarding, IT Access | — | — | — | — | **O** (admin) | — | — | A (IT access) | A (confirm) | C (provisioning) |
| 17 | IT & Systems Admin | — | — | — | — | R | — | — | A (governance) | R | **O** (tier-1, target) |
| 18 | Compliance / Insurance | — | — | — | R (tender evidence) | **O** | — | — | A | A (annual) | — |
| 19 | Fleet | — | — | — | — | **O** | C (driver) | — | A (insurance) | R | — |
| 20 | Marketing & Brand | C (consent) | — | — | C (consent) | — | — | **O** | — | A | — |
| 21 | Document Management | — | — | — | R | **O** (residual) | R | R | R | R | — |

---

## Read across

- **P10 Finance Director** is the busiest role on the matrix — owner on five workflows and approver on most of the rest. Aligns with the audit's finding that finance workloads (AP, inbox, cashflow) dominate current monthly hours.
- **P06 Project & Commercial Lead** owns the project lifecycle group (02–05, 07) and is approver on the rest of it.
- **P07 Office & Compliance Coordinator** owns the operational glue — compliance, comms, materials, fleet, document upkeep.
- **External roles** (P01 Architect, P03 Subbie) are mostly source/recipient on JPMS, never owner. This shapes the external-portal scope: read what's published to them, write only their own contributions.
- **P12 Outsourced IT Helpdesk** is the only role whose JPMS surface is deliberately narrow — provisioning hooks and audit reports only.

---

## Process for refining this matrix

1. When a workflow file moves from **Draft** → **In Review**, walk the row above with the named operational owner.
2. Capture per-step CRUD permissions inside the workflow file (and inside derived user-journey files).
3. Update the relevant cell in this matrix.
4. When all rows are confirmed, root [`README.md`](../../README.md) §4.7 "Permission matrix populated" can be ticked.
