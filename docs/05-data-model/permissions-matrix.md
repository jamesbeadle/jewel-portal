# Permissions Matrix — Role × Workflow

Coarse-grained matrix of who is responsible for what across the ten workflows. Per-step CRUD permissions live in each workflow file.

**Legend:** **O** Owner · **A** Approver · **C** Contributor · **R** Read · **—** none.

## Roles

| ID | Role |
|---|---|
| P01 | Director / MD |
| P02 | Finance Director |
| P03 | Project Manager |
| P04 | QS / Estimator |
| P05 | Site Manager |
| P06 | Health & Safety Officer |
| P07 | Office & Compliance Coordinator |
| P08 | Architect / Designer / Consultant |
| P09 | Client / Homeowner |
| P10 | Subcontractor |
| P11 | Foreman / Site Team |

## Matrix

| # | Workflow | P01 Dir | P02 FD | P03 PM | P04 QS | P05 SM | P06 H&SO | P07 OCC | P08 Architect | P09 Client | P10 Subcontractor | P11 Foreman |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 00 | Sales, Marketing & CRM | A (Won / high-value) | R | **O** | C | C (site visits) | — | — | C (referrals) | C (origin) | — | — |
| 01 | Drawing Receipt & Document Control | R | — | **O / A** | R | R (mobile) | R | — | C (source) | R | R | R (mobile) |
| 02 | Pre-Construction: Tender & BoQ | A | R | C | **O** | C (walk-round) | — | — | C (source) | — | — | — |
| 03 | Subcontractor Procurement & Onboarding | A (high value) | R | **O / A** | C | — | A (compliance gate) | **O** (onboarding records) | — | — | C (quote + compliance) | — |
| 04 | H&S Site Mobilisation & Compliance | A (annual review) | — | C (paperwork) | — | **O** (on-site confirmation) | **O** (framework) | C (filing) | — | — | C (acceptance) | — |
| 05 | RFIs, Submittals, Variations & Delays | A (high value) | R | **O** | C (variation pricing) | C (raises from site) | — | — | **A** (RFI/submittal/VO sign-off) | A (variation client side) | C (source) | C |
| 06 | Site Delivery, Programme & Reporting | R | — | A (project oversight) | C (programme update) | **O** | C (observations) | — | R (live dashboard) | R (live dashboard) | C | C (capture) |
| 07 | Valuations, Cashflow & Forecasting | A | **O** | C (triggers valuation) | **O** (valuations) | C (timesheet capture) | — | — | R (Programme Valuation Report) | R (scoped) | C (day-rate timesheets) | C (timesheets) |
| 08 | Quality, Snags, Handover & Aftercare | A (final VAT) | **O** (settlement / VAT / retention release) | A (commercial sign-off) | C (final account) | **O** (snag coordination) | A (H&S close-out) | C (compliance archive) | A (close-out review) | A (Practical Completion, defects sign-off) | C (resolves snags) | C (snag capture) |
| 09 | Portfolio Reporting & Analytics | **Audience** | **O** | R (scoped) | R (commercial slice) | R (own projects) | R (H&S slice) | R | — | — | — | — |

## Read across

- **P03 PM** is the busiest project-side role — owner on 00, 01, 05; co-owner / approver on 03, 06.
- **P02 FD** owns the financial output side — 07 and 08 (settlement / VAT). Their accountancy day-job remains outside JPMS.
- **P05 Site Manager** owns the live-site experience (06) and shares ownership of H&S mobilisation (04) with the H&SO.
- **P06 H&SO** owns the H&S framework and engine; not the daily running of site.
- **P07 OCC** owns the compliance paperwork backbone — onboarding records and document register.
- **External roles** (P08 Architect, P09 Client, P10 Subcontractor) are mostly source / approver / read; never owner of an internal workflow.
- **P11 Foreman** is data-capture; never an owner.
