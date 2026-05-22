# 03 — Workflows

Ten detailed workflows, one per lifecycle stage. Each file follows the same shape: purpose, current state, target flow, JPMS functionality required, user stories (with `US-NN-MM` IDs and status flag), integrations, acceptance criteria, entities touched, roles involved, open questions, confirmation checklist.

For the high-level lifecycle view, see [`/02-lifecycle/`](../02-lifecycle/).

| # | Workflow | File | Owner | User stories | Status |
|---|---|---|---|---|---|
| 00 | Sales, Marketing & CRM | [`00-sales-marketing-crm.md`](00-sales-marketing-crm.md) | P03 PM | 14 | Draft |
| 01 | Drawing Receipt & Document Control | [`01-drawing-receipt-document-control.md`](01-drawing-receipt-document-control.md) | P03 PM | 8 | Draft |
| 02 | Pre-Construction: Tender & BoQ | [`02-preconstruction-tender-boq.md`](02-preconstruction-tender-boq.md) | P04 QS | 9 | Draft |
| 03 | Subcontractor Procurement & Onboarding | [`03-subcontractor-procurement-onboarding.md`](03-subcontractor-procurement-onboarding.md) | P03 PM / P07 OCC | 22 | Draft |
| 04 | H&S Site Mobilisation & Compliance | [`04-hs-site-mobilisation-compliance.md`](04-hs-site-mobilisation-compliance.md) | P06 H&SO + P05 Site Manager | 16 | Draft |
| 05 | RFIs, Submittals, Variations & Delays | [`05-rfis-submittals-variations-delays.md`](05-rfis-submittals-variations-delays.md) | P03 PM | 12 | Draft |
| 06 | Site Delivery, Programme & Reporting | [`06-site-delivery-programme-reporting.md`](06-site-delivery-programme-reporting.md) | P05 Site Manager | 12 | Draft |
| 07 | Valuations, Cashflow & Forecasting | [`07-valuations-cashflow-forecasting.md`](07-valuations-cashflow-forecasting.md) | P02 FD / P04 QS | 32 | Draft |
| 08 | Quality, Snags, Handover & Aftercare | [`08-quality-snags-handover-aftercare.md`](08-quality-snags-handover-aftercare.md) | P05 Site Manager + P02 FD | 20 | Draft |
| 09 | Portfolio Reporting & Analytics | [`09-portfolio-reporting-analytics.md`](09-portfolio-reporting-analytics.md) | P02 FD (audience: Directors) | 12 | Draft |

**Total: 157 user stories.**

## Cross-workflow modules

Some capabilities thread through multiple workflows. They are documented inside the workflow that owns the framework, but used elsewhere:

- **Inspections engine** — owned by [04](04-hs-site-mobilisation-compliance.md); used by 04 (H&S), 06 (quality observations), 08 (snag inspections).
- **Observations / Incidents / Corrective Actions** — owned by [04](04-hs-site-mobilisation-compliance.md); observations also surface in 06.
- **Submittals & Approvals** — captured in [05](05-rfis-submittals-variations-delays.md); approvals route to architects (P08) before installation.
- **Correspondence / Instruction Log** — appears in [01](01-drawing-receipt-document-control.md) and [05](05-rfis-submittals-variations-delays.md); references the meeting minutes and action tracker captured against the project.
- **Punch / Snag management** — owned by [08](08-quality-snags-handover-aftercare.md); identification can happen anywhere from 06 onwards.
