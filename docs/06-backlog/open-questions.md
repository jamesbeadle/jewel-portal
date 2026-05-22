# Open questions

Consolidated from the workflow files. Each workflow file retains its own Open Questions list; this is the cross-cutting view.

## Authentication & users
- E-signature provider — DocuSign / Adobe Sign / M365 native?
- Subcontractor portal authentication — magic link per project, or persistent account?
- Architect / client portal scoping per contract — opt-in default vs always-on?

## 00 — CRM
- Lead-source attribution — closed list of channels, or free-text + dedupe?
- Site visit booking — sync with which calendar (Outlook, Google)?
- Proposal templating — single template or per-project-type variants?
- Nurture cadence — fixed (e.g. 30 / 90 / 180 days) or per-lead?

## 01 — Drawing & Doc Control
- Drawing supersedure — what's the fall-back when auto-extract fails?
- Retention policy on superseded revisions (forever, project lifetime, or N years post close-out)?

## 02 — Tender & BoQ
- Bluebeam — direct API integration available, or ship via export/import file?
- AI rate suggestion — confidence threshold to auto-apply vs flag for review?

## 03 — Procurement & Onboarding
- Approval threshold for award sign-off — when does Director sign-off kick in?
- Hard block vs soft warn at compliance expiry?

## 04 — H&S Mobilisation
- Mobilisation gate strictness — hard block until 100% green, or allow Director override?
- Inspection template versioning — retain exact template version per instance?
- Toolbox-talk content — managed inside JPMS or linked from external library?

## 05 — RFIs, Submittals, Variations, Delays
- Approval thresholds — at what value does the Director approve variations?
- RFI chase cadence — how many days before auto-reminder, to whom?

## 06 — Site Delivery, Programme & Reporting
- QR vs geofence attendance — acceptable to subcontractors?
- Offline storage cap on the device — at what point ask the user to sync?
- Photo retention — full-resolution on server, thumbnails on device?

## 07 — Valuations, Cashflow & Forecasting
- Cost-code overrun — hard-block vs soft-warn-and-proceed with FD sign-off?
- Are timesheets allocated per day or per task within a day?
- Forecast horizon — 13-week rolling, or different per audience?
- Completion-% prediction model — site-reported %, timesheet burn rate, or blended?
- Snapshot per Claim Period — retained for historical comparison?

## 08 — Quality, Snags, Handover & Aftercare
- Practical Completion trigger — single PC event firing both 07-defects and 08-settlement, or two events?
- Zero-rated VAT analysis — always required, or only certain contract types?
- Retention release — waits for both close-out and settlement, or just settlement?
- Disputed VAT outcome — how many revision rounds before escalating to Directors?

## 09 — Portfolio Analytics
- Threshold defaults — start conservative, tune later?
- Director view scoping vs FD view — fewer controls, same data?
- External reporting — board packs, lender reporting?
