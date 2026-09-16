# P04 — Quantity Surveyor / Estimator

**Type:** Internal (sometimes external consultant)
**Reports to:** Directors / MD
**Frequency on platform:** Daily during tender / valuation cycles; ad hoc on variations
**Status:** Draft

## Goals
- Build accurate tenders fast, on a re-usable rate library, with quantities read off the drawing itself — never measured by hand, never re-keyed from a take-off tool.
- Work against a single canonical drawing in the JPMS drawing register — never chase the latest revision, never upload it twice.
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
- **Drawings land in the JPMS drawing register as they arrive** (workflow 01) — filed from the projects mailbox through Document Triage, or uploaded as a revision — and every revision is transcribed as it lands. The register IS the drawing store.
- **Take-off comes from the drawing itself, not from Bluebeam** (decided 2026-09-07; workflow 02). The portal reads each PDF's own vector geometry and positioned text (`api/Features/Drawings/Geometry`): the title block, the revision table, the sheet's proven scale, and every figured dimension, callout and closed shape as rows the assistant can query and total. Measurement is exact at the proven scale and depends on no outside connection. Bluebeam's Markups API is not a take-off engine — it returns only markups a person has drawn in Revu, and nothing at all on a drawing nobody has marked up — so Revu markups are an optional import that exists only when someone has genuinely measured in Revu. The earlier plan (Markups List CSV in v1, Markups API in phase 2, a JPMS tool-set with a cost-code column) is withdrawn.

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
