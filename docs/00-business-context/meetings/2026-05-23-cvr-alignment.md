# Meeting: CVR alignment — JPMS delivers the CVR surface; Planyard subscription not required

**Date:** 2026-05-23
**Attendees:** Nigel Reilly (project lead) — reviewing email from James Clark (QS at JBB) and the business owner.

---

## Context

JBB has been piloting Planyard as a replacement for the Excel CVR workbook. Two CVR artefacts were sent for review:

- **April 26 - By France.xlsx** — the existing CVR (Header Sheet, Sub-Contractors, Sub Contract Analysis, QS Accruals, Prelim Forcast). Built around clear forecast traceability and per-package margin visibility.
- **By France - Workbook.xlsx** — the new pilot workbook (12 sheets including Valuation, Variations, CVR, Subcontractors, Dayworks, Contra Charges, Sub Retention, Cash Flow, CEO Dashboard). Planyard-style.

James Clark raised three specific issues with the pilot that the old CVR handled better. The business owner asked whether JPMS could deliver the CVR surface itself rather than the team paying for Planyard.

---

## Decisions

| # | Decision | Date |
|---|---|---|
| D1 | **JPMS delivers the CVR as a third primary commercial output of workflow 07**, alongside the Programme Valuation Report and the cashflow forecast. All three are built from the same project data, so they cannot disagree. | 2026-05-23 |
| D2 | **Planyard subscription is not required.** Planyard is added to the "what JPMS replaces" list in [`/05-data-model/integrations.md`](../../05-data-model/integrations.md) with a note that JPMS delivers the same surface plus the three improvements James called out. | 2026-05-23 |
| D3 | **Fix #1 — Forecast Final Cost is traceable.** Every CVR Forecast Final Cost number is the sum of explicit components — Cost Incurred / Cost Committed / QS Accruals / Prelim Forecast / Cost to Complete — and drills into each component. Never a black box. JPMS replicates the old CVR's QS Accruals sheet (Add / Omit / Liability per category with sign-off) and the old CVR's Prelim Forcast sheet (week × item grid with Tendered £, Actual £, Difference £). | 2026-05-23 |
| D4 | **Fix #2 — Prelims and EOTs visible against tender separately.** Prelims live as a distinct CVR section above the BoQ packages. Each prelim item shows Tendered / Actual / Forecast / Difference. The CVR header carries the time-control panel: Contract Programme dates, EOT count, Anticipated Completion vs Contract Completion, **Weeks Ahead / Behind**. Time-related prelim overspend (weeks late × weekly prelim run rate) is calculated automatically and surfaced as a distinct line. An EOT register holds each Extension of Time with reason, period granted, programme impact and commercial recovery position. | 2026-05-23 |
| D5 | **Fix #3 — Variations against original BoQ headings AND on the central register.** Both views surface from the same underlying variation data. Every Variation carries a BoQ package / cost-code link. The per-package CVR view shows each row as Order (Cost / Value / Profit £ / %) + Variation (Cost / Value / Profit £ / %) + Combined, so margin per package is visible including variations. The central Variations Register (workflow 05) remains for status / certification tracking. The same data answers both questions. | 2026-05-23 |
| D6 | **CVR Movement column** showing £ change since the prior CVR snapshot per line, so monthly reviews are "what moved this period?" rather than re-reading absolute values. | 2026-05-23 |
| D7 | New entities added to the data model: **CVR Snapshot, Forecast Component, Margin Trace, QS Accrual, Prelim Item, Prelim Forecast Entry, EOT, Daywork, Contra Charge, Subcontractor Retention.** | 2026-05-23 |

---

## What James Clark called out (and JPMS now addresses)

Quoting from James's email to Nigel and Kye (2026-05-21 12:31):

> "Forecasting — The workbook is pulling forecast final cost and margin from multiple linked tabs and assumptions, whereas the previous CVR has clear QS Accruals and a Prelim Forecast sheet, so you can see exactly how the forecast made up."

**JPMS response:** Fix #1 above. Forecast Final Cost = sum of Forecast Components (Cost Incurred, Cost Committed, QS Accruals, Prelim Forecast, Cost to Complete), each drillable. QS Accruals module and Prelim Forecast module both modelled explicitly.

> "Prelims and EOTs — In the previous CVR, prelims and EOT costs are clearly shown against the tender, but in the workbook prelims just sit inside the general BOQ/cost, so overspends and time related losses are much less obvious."

**JPMS response:** Fix #2 above. Prelims are a distinct section above BoQ packages with per-item Tendered / Actual / Difference. EOT register on the project. CVR header shows Weeks Ahead / Behind. Time-related prelim overspend calculated automatically.

> "Variations — The workbook keeps all variations on a central VO tab and feeds them in as separate lines, rather than against the original BOQ/tender headings, which makes it harder to see margins by subcontract package. It also makes it difficult to see which trades are over or under their % and profitability once variations are included."

**JPMS response:** Fix #3 above. Variations carry a BoQ package link. Per-package CVR view shows Order + Variation + Combined columns. Central register remains, but it doesn't have to be either/or — both views from the same data.

---

## Artefacts updated

- [`/03-workflows/07-valuations-cashflow-forecasting.md`](../../03-workflows/07-valuations-cashflow-forecasting.md) — fully rewritten to deliver three outputs (PVR, CVR, Cashflow) with 45 user stories. Stories US-07-23 through US-07-35 are CVR-specific and map to the three fixes above.
- [`/05-data-model/entities.md`](../../05-data-model/entities.md) — Workflow 07 section expanded with CVR-supporting entities (CVR Snapshot, Forecast Component, Margin Trace, QS Accrual, Prelim Item, Prelim Forecast Entry, EOT, Daywork, Contra Charge, Subcontractor Retention). ERD updated.
- [`/05-data-model/integrations.md`](../../05-data-model/integrations.md) — Planyard entry expanded to call out the three fixes. Excel CVR workbook added to the replaced-by-JPMS list.
- Root [`README.md`](../../../README.md) — Section 1 names the three outputs; Section 3 highlights workflow 07 producing all three; Section 4 adds CVR + Forecast Component + QS Accrual + Prelim Item + EOT to the Domain Concepts list; Section 5 adds the CVR alignment pass to Done.

---

## Open questions surfaced

- [ ] CVR review cadence — monthly with the QS lead, or weekly for active projects?
- [ ] EOT commercial recovery — captured against the EOT entry, or routed through workflow 05 variations?
- [ ] Time-related prelim run rate — derived from the Prelim Forecast week × item grid automatically, or set per project at contract setup?
- [ ] QS Accruals sign-off — does it require Director sign-off above a threshold, like variations?

---

## What we are NOT doing

- Building a separate CVR module outside workflow 07. The CVR shares its data source with the PVR and cashflow forecast; making it a separate module would re-introduce the reconciliation problem the unified workflow exists to solve.
- Subscribing to Planyard. JPMS delivers the surface, plus the fixes above.
- Replicating every Planyard feature. CIS / VAT calculations remain in Xero (accountancy) where they belong — JPMS publishes the data Xero needs.
