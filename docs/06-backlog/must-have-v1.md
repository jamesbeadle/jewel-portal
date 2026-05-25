# Must-have for v1

For v1, we ship: correct role modelling, clear ownership boundaries, the right business language, clean handoffs between stages, enough workflow depth to protect delivery and cashflow.

## Foundations
- OAuth sign-in (Google, Microsoft, email/password), invitation-by-admin/PM.
- ASP.NET Core API + Azure SQL.
- Admin-managed user directory across the 11 personas.

## Lifecycle workflows (v1 cut)
- **00 CRM** — pipeline stages, lead capture, site visit, drawings chase, bid/no-bid, proposal, won/lost, project-shell handoff to 01.
- **01 Drawing & Doc Control** — **Bluebeam Studio Projects as the canonical drawing store** (Studio Projects API + webhooks; the QS never re-uploads a drawing), drawing register, revisions, issue records, acknowledgments, fall-back email-inbox channel.
- **02 Tender & BoQ** — Bluebeam take-off via Markups List CSV import (JPMS publishes a Bluebeam tool-set with a cost-code custom column so every take-off is tagged at source), rate library, BoQ module, M&E discipline tagging, re-tender comparison.
- **03 Procurement & Onboarding** — bid package builder, comparison & award, work order generation, **subcontractor compliance gate** before award (insurance / RAMS / CIS).
- **04 H&S Mobilisation** — mobilisation checklist as a hard gate, inspections engine, observation/incident/corrective-action loop, toolbox talks, RAMS acceptance via portal.
- **05 RFIs, Submittals, Variations, Delays** — unified change register, **submittal approvals**, VO → procurement loop, RFI portal for architects.
- **06 Site Delivery, Programme & Reporting** — site app (progress, photos, attendance, snags), programme module, weekly/monthly report assembly.
- **07 Valuations, Cashflow & Forecasting** — three primary outputs: **Programme Valuation Report** per Claim Period; **CVR (Cost-Value Reconciliation)** with traceable forecast components (Cost Incurred / Cost Committed / QS Accruals / Prelim Forecast / Cost to Complete), Prelims and EOTs visible against tender separately with weeks-ahead/behind tracker, per-package variation margin alongside the central register; **cashflow forecast** from project data alone. Plus timesheet management with cost-code hard-block. **Removes the need for Planyard.**
- **08 Quality, Snags, Handover & Aftercare** — defect register, completion pack, Practical Completion, **zero-rated VAT analysis** agreed with client, retention release, defects-period tracking.
- **09 Portfolio Reporting & Analytics** — Director dashboard, FD dashboard, leading indicators, threshold alerts.

## Critical cross-cutting
- Inspections engine (used by 04, 06, 08).
- Observations / Incidents / Corrective Actions engine (owned by 04, used cross-cutting).
- Correspondence / Instruction log (project-tied).
- Mobile-first site app (workflows 04, 06, 08).
- Client portal scoped views (selected fields from 05, 06, 07, 08).
- Subcontractor portal (workflows 03, 04, 05, 06, 07).
