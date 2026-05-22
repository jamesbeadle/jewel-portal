# 02 — Lifecycle

JPMS runs the full project lifecycle from the first lead touchpoint through aftercare and portfolio reporting. Ten stages, in order. Each stage has a brief description here and a detailed workflow file in [`/03-workflows/`](../03-workflows/).

| Stage | Lifecycle stage | Detailed workflow |
|---|---|---|
| 00 | [Sales, Marketing & CRM](00-sales-marketing-crm.md) | [`/03-workflows/00-sales-marketing-crm.md`](../03-workflows/00-sales-marketing-crm.md) |
| 01 | [Drawing Receipt & Document Control](01-drawing-receipt-document-control.md) | [`/03-workflows/01-drawing-receipt-document-control.md`](../03-workflows/01-drawing-receipt-document-control.md) |
| 02 | [Pre-Construction: Tender & BoQ](02-preconstruction-tender-boq.md) | [`/03-workflows/02-preconstruction-tender-boq.md`](../03-workflows/02-preconstruction-tender-boq.md) |
| 03 | [Subcontractor Procurement & Onboarding](03-subcontractor-procurement-onboarding.md) | [`/03-workflows/03-subcontractor-procurement-onboarding.md`](../03-workflows/03-subcontractor-procurement-onboarding.md) |
| 04 | [H&S Site Mobilisation & Compliance](04-hs-site-mobilisation-compliance.md) | [`/03-workflows/04-hs-site-mobilisation-compliance.md`](../03-workflows/04-hs-site-mobilisation-compliance.md) |
| 05 | [RFIs, Submittals, Variations & Delays](05-rfis-submittals-variations-delays.md) | [`/03-workflows/05-rfis-submittals-variations-delays.md`](../03-workflows/05-rfis-submittals-variations-delays.md) |
| 06 | [Site Delivery, Programme & Reporting](06-site-delivery-programme-reporting.md) | [`/03-workflows/06-site-delivery-programme-reporting.md`](../03-workflows/06-site-delivery-programme-reporting.md) |
| 07 | [Valuations, Cashflow & Forecasting](07-valuations-cashflow-forecasting.md) | [`/03-workflows/07-valuations-cashflow-forecasting.md`](../03-workflows/07-valuations-cashflow-forecasting.md) |
| 08 | [Quality, Snags, Handover & Aftercare](08-quality-snags-handover-aftercare.md) | [`/03-workflows/08-quality-snags-handover-aftercare.md`](../03-workflows/08-quality-snags-handover-aftercare.md) |
| 09 | [Portfolio Reporting & Analytics](09-portfolio-reporting-analytics.md) | [`/03-workflows/09-portfolio-reporting-analytics.md`](../03-workflows/09-portfolio-reporting-analytics.md) |

## The handoff principle

The single most important design rule across the lifecycle: information entered once should flow forward without re-keying. The biggest specific handoff is **00 → 01** (sales-won opportunity becomes a project shell), but the same principle applies between every adjacent stage.

## What feeds what

```
Sales/CRM (00)
  └─→ Drawing & Doc Control (01) ← Architect issues drawings
        └─→ Tender & BoQ (02)
              └─→ Procurement & Onboarding (03)
                    └─→ H&S Mobilisation (04) ← H&SO + Site Manager gate before work starts
                          └─→ Site Delivery / Programme / Reporting (06) ⇄ RFIs / Submittals / Variations / Delays (05)
                                └─→ Valuations / Cashflow / Forecasting (07) ⇄ Cost-code allocation from timesheets
                                      └─→ Quality / Snags / Handover / Aftercare (08)

Cross-cutting across the lifecycle:
  Portfolio Reporting & Analytics (09) — reads from every stage above
```
