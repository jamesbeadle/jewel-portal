# 01 — User Roles

Eleven user roles for JPMS. Each role has its own card. The role × workflow responsibility matrix is in [`/05-data-model/permissions-matrix.md`](../05-data-model/permissions-matrix.md).

| # | Role | Type | Card |
|---|---|---|---|
| P01 | Director / MD | Internal executive | [`01-director-md.md`](01-director-md.md) |
| P02 | Finance Director | Internal executive | [`02-finance-director.md`](02-finance-director.md) |
| P03 | Project Manager | Internal | [`03-project-manager.md`](03-project-manager.md) |
| P04 | Quantity Surveyor / Estimator | Internal | [`04-quantity-surveyor-estimator.md`](04-quantity-surveyor-estimator.md) |
| P05 | Site Manager | Internal field | [`05-site-manager.md`](05-site-manager.md) |
| P06 | Health & Safety Officer (H&SO) | Internal | [`06-health-safety-officer.md`](06-health-safety-officer.md) |
| P07 | Office & Compliance Coordinator | Internal | [`07-office-compliance-coordinator.md`](07-office-compliance-coordinator.md) |
| P08 | Architect / Designer / Consultant | External | [`08-architect-designer-consultant.md`](08-architect-designer-consultant.md) |
| P09 | Client / Homeowner | External | [`09-client-homeowner.md`](09-client-homeowner.md) |
| P10 | Subcontractor | External delivery partner | [`10-subcontractor.md`](10-subcontractor.md) |
| P11 | Foreman / Site Team | Internal field | [`11-foreman-site-team.md`](11-foreman-site-team.md) |

## Role boundary clarifications

These are the boundaries that matter most because the business needs them clean:

- **P05 Site Manager vs P11 Foreman / Site Team.** Site Manager owns live-site delivery (sequencing, quality checks, snag coordination, RAMS enforcement, escalation, H&S confirmation on site). Foreman / Site Team owns the workface — daily progress input, photos, attendance, housekeeping, immediate issue reporting up to the Site Manager.
- **P06 H&SO vs P07 OCC.** H&SO owns the H&S framework, the audit regime, inspection templates, incident governance, corrective action oversight, mobilisation H&S, and coaching. OCC owns the admin: subcontractor onboarding records, insurance/cert/CIS/RAMS collection, expiry tracking, filing, register maintenance. OCC does not own live-site safety; H&SO does not own office admin.
- **P03 PM vs P04 QS/Estimator.** PM owns programme, drawings, client liaison, coordination, the paperwork side of H&S. QS/Estimator owns tender build-up, package comparisons, award support, valuations, variations pricing and final account.
- **P08 Architect vs P09 Client.** Architect/Designer/Consultant is the technical author and reviewer. Client / Homeowner is the end customer who selects, approves, instructs, pays and signs off.
