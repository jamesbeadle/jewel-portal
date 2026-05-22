# Commercial model

The commercial structure JPMS reflects.

## Project lifecycle and money
- **Tender / BoQ** (workflow 02) establishes the project's contract value as a line-item priced BoQ.
- **Variations** (workflow 05) update the BoQ over the life of the project.
- **Claim Period** (typically monthly, contract-specific) is the cycle on which the **Programme Valuation Report** is issued (workflow 07). Each Programme Valuation Report carries the **Claim Value** for that period.
- **Cashflow forecast** (workflow 07) is built from project data — forward work-order commitments, expected valuations, predicted completion %s, retention timing.
- **Settlement Record** (workflow 08) closes the project commercially at PC; includes the **zero-rated VAT analysis** agreed with the client.
- **Retention** is released as part of settlement once close-out is signed off.

## Cost codes
Architect-defined client-facing cost codes thread through every screen and report touching their project. Timesheet allocation, work orders, valuation roll-ups all carry cost codes.

## Cross-entity (BB / PS / PFP)
Every transaction and commitment carries a cross-entity flag at source so the consolidated and per-entity views are both accurate.

## What's NOT in JPMS commercially
AP, AR, payroll, statement reconciliation, payment runs, VAT submissions to HMRC, retention transactions in the ledger. These all run in the accountancy team's tools (Xero, Brightpay, Dext, Chaser HQ) using JPMS-published data downstream.
