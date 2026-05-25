# P04 — Quantity Surveyor / Estimator

**Type:** Internal (sometimes external consultant)
**Reports to:** Directors / MD
**Frequency on platform:** Daily during tender / valuation cycles; ad hoc on variations
**Status:** Draft

## Goals
- Build accurate tenders fast, on a re-usable rate library, with quants imported from Bluebeam without leaving Bluebeam to do it.
- Work against a single canonical drawing in Bluebeam Studio Projects — never chase the latest revision, never upload it twice.
- Compare returned subcontractor quotes on one screen and award without rebuilding the comparison in Excel.
- Run the valuation cycle from contract + approved variations + current % without rekey.
- Price variations against the rate library.
- Manage the final account at project close.

## Pain points (current state)
- Drawings arrive by email, get saved to SharePoint, re-uploaded to Buildertrend, then opened in Bluebeam — the QS handles the same file three times.
- Take-off in Bluebeam → exported to Excel → loose rate research → standalone BoQ that nobody else sees.
- Subcontractor quote comparisons re-built per tender.
- Valuation rebuilt every claim period.

## How JPMS changes this for the QS
- **Drawings flow into JPMS from Bluebeam Studio Projects automatically** (workflow 01). The Studio Project IS the drawing store; the QS opens drawings in Bluebeam Revu directly from the JPMS drawing register. No re-upload, no manual file moves.
- **Take-off lands in JPMS via Bluebeam Markups List CSV import in v1** (workflow 02), then via the Bluebeam Markups API direct in phase 2. JPMS publishes a Bluebeam tool-set with a JPMS cost-code column on every take-off markup, so the take-off is tagged to its BoQ destination at source.

## Involvement across workflows
- **Owner on:** 02 (Pre-Construction: Tender & BoQ), 07 (Valuations slice — produces the Programme Valuation Report each Claim Period), 08 (final account slice within close-out).
- **Contributor on:** 03 (procurement — produces the bid packages PM runs the award flow on; advises on award), 05 (prices variations against the rate library), 09 (commercial portfolio data).
- **Read on:** 06 (site reality feeds completion %), 04 (mobilisation cost), 01 (drawings drive BoQ).

## Permissions (coarse)
- Owner on BoQ, rate library, valuations, variation pricing, final account.
- Approver on bid comparisons (recommendation; PM / Director sign-off depending on value).

## Devices
- Desktop primarily; tablet for site walk-rounds with the PM.

## Notes
- Where Jewel uses an external QS consultant, they take this persona for the engaged project — invited as a JPMS user scoped to that project.
