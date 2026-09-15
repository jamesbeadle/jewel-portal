# H&S site audit in the portal — scope (2026-09-15)

**Trigger:** Katy-Louise Hicks (H&SO) emailed her By France audit on 1 Sep 2026 (score 89%, "well organised and neat as always") as `H&S Inspection Report Framework 27 aug 26.xlsm`. Nigel replied 2 Sep copying James Beadle: "how we can make the attached work in the portal on the H&S phase". This doc is the scoping for that build; the email thread is the only source so far — nothing has been walked through with Katy yet.

**Decisions (Nigel, 15 Sep):**
1. First build = **audit form + actions**: Katy's template as a per-project portal form, auto-scored, every flagged finding becoming a corrective action with an owner and a due date; the existing `HsRecord` register gets its page at the same time.
2. Findings with an owner are **H&S corrective actions** (`HsRecord` kind `CorrectiveAction`) — not to-dos.
3. Scoring **matches the spreadsheet** exactly: `score = max(0, (Σrate − Σminus) ÷ (rated items × 10))`, Minus hand-entered, unrated rows excluded from the denominator. Class penalties in her key (A −25% … D −1%, R −5%) are *not* applied automatically. Her By France figure reproduces: 38 rated, 340/380 = 89.5% → "89%", Good.
4. **Import the 1 Sep By France audit** as the first record so the page isn't empty and the thread's actions land on the right people.

## What the spreadsheet is (the template)

- Header: site, site location, site manager, safety officer, safety director, PM, contracts manager, inspection date, serial no., type of report (Initial / Routine / Follow-Up / Final / Other), job no., contract end date, previous audit score, summary of activities, customer responsibility, principal contractor, contractor, no. of operatives.
- **182 checklist items in 11 sections**: 1 Documentation (25) · 2 Temporary Works (6) · 3 Notices & Posters (6) · 4 Specific Training (15) · 5 General (32) · 6 Environment (11) · 7 Work Activities (51) · 8 Working at Heights (11) · 9 Traffic Management (9) · 10 Fire & Emergency (12) · 11 Offices & Welfare (4).
- Per item: comment code (N/A, N, N/C, N/S, R), rate (0 / 5 / 10), class (A–E), minus, time-scale (I, 1, 3, 7, 1M, O), findings text, owner initials, date rectified.
- Rating bands: <70% Poor (report required) · 70–84% Fair · 85–94% Good · 95%+ Very Good.
- Footer: accident / near-miss review (5 rows), further comments, safety-officer declarations (true reflection; explained to the manager), manager declaration that actions are complete (name, position, signature, date).
- A VBA button `Archive_Submit_Clear_Final` copies the header and every row with a time-scale into an `H&S_Archive` sheet and clears the form — i.e. Katy has hand-built the audit + actions database the portal should be.

Two wrinkles: the class penalties are described but the formula only reads the hand-typed Minus column (empty on this audit); and no row on this audit has a time-scale, so her own archive macro would have captured zero actions. Both are why the portal should derive the actions from *owner or rate < 10*, not from time-scale alone.

Machine-readable extracts, in the repo: `docs/03-workflows/04-hs/hs-audit-template.json` (sections, items, keys, scoring rule) and `hs-audit-by-france-2026-09-01.json` (this audit: header, 45 populated rows, 8 findings, the thread's decisions, people not yet in the portal).

## What the portal already has

- **`HsRecord`** (api `Features/Hs`, entity `HsRecordEntity`): kinds Observation / NearMiss / Incident / CorrectiveAction / ToolboxTalk / Permit; severity Low–Critical; status Open / InProgress / Closed; `AssignedToEmail`; RaisedAt / DueAt / ClosedAt; plus `HsRecordAttendance` (attendee + signature blob) for toolbox talks. Routes `/api/hs-records`, connector actions `log_hs_record`, `update_hs_record`, `record_attendance_for_hs_record` (gate: Director, PM, SiteManager, HealthAndSafetyLead). `PhotoAttachedKind.HsRecord` exists.
- **No page renders any of it.** The sidebar's only "H&S" is Subcontractor → H&S, a mail category (`/subcontractors/communications/h-s`). The `HealthSafetyOfficer` role's home says "Open actions and anything due for inspection" and shows nothing.
- Workflow 04 (`docs/03-workflows/04-hs-site-mobilisation-compliance.md`) already specifies an Audits engine (US-04-06: formal audit on tablet with findings, owners, close-out) and a corrective-action register — Drafted, not built.
- **Building Control** is the built precedent to mirror: per-project page under the Project folder, a case with an inspection ladder, staged from an email in the Control Centre (`create_building_control_inspection_from_message`), attachments store, connector read + actions.

## The build

**Data.** New `HsAudit` (per project: template version, serial no. minted per project like WOs, inspection date, type of report, site manager, safety officer, header fields, summary, score, rating, officer/manager declarations, status Draft → Issued → Closed) and `HsAuditItem` (one per template item: code, section, comment, rate, class, minus, time-scale, findings, owner email, date rectified, linked `HsRecordId`). The template lives in code as a versioned constant seeded from `hs-audit-template.json` (workflow 04's open question on template versioning: yes — the audit records the template version it was run against). Migration + snapshot.

**Scoring** in one pure class (`HsAuditScoring`) with the spreadsheet formula and bands; tests pin the By France 89% and the edge cases (no rated rows → blank, minus > sum → 0).

**Actions.** On Issue, every item with an owner *or* rate < 10 (and not N/A) becomes an `HsRecord` `CorrectiveAction` on the project: summary "`code name` — findings", severity from class (A/B → Critical/High, C → Medium, D/E → Low), due date from time-scale (I/1 → +1 day, 3, 7, 1M → +30, O → none), assignee from owner. The item keeps the link; closing the record stamps the item's date rectified, and the audit closes when every linked record is closed (the manager's declaration). Re-issuing doesn't duplicate — the item's existing link is updated.

**Pages.** Project folder → **H&S** (`/projects/{project}/hs`): tabs Audits · Actions · Register (observations, near misses, incidents, toolbox talks, permits — finally rendering `HsRecord`). Audit detail (`/projects/{project}/hs/audits/{auditId}`) is the form: header, sections as collapsible groups, tap-friendly 0/5/10 and A–E pickers, live score chip with band colour, findings text, owner picker (portal people), declarations. Built for tablet — Katy fills it on site. Previous audit score auto-filled from the last issued audit on the project. Printable report matching her layout.

**Role home.** `HealthSafetyOfficer` home tiles: open corrective actions by project, audits due (last audit + interval), recent incidents — US-04-15 in miniature.

**Connector.** Reads: `get_hs_audit`, `list_hs_audits`, `list_hs_records` (the register was write-only over the connector until now — "look before you say cannot"). Actions: `create_hs_audit`, `update_hs_audit_items` (full-list rule), `issue_hs_audit` (confirm-first, mints the corrective actions), `close_hs_audit`. Triage: `create_hs_audit_from_message` for the next time Katy emails one — stages the audit with the workbook attached, like Building Control's inspection-from-message.

**Import of this audit.** A one-off seed from `hs-audit-by-france-2026-09-01.json` on By France (`3490f944b29545c4b8d5a04130f42ab8`), dated 1 Sep, issued, raising these corrective actions:

| Item | Action | Owner | Note |
|---|---|---|---|
| 1.14 Toolbox Talks | Run a toolbox talk in September | JE | rate 5, D |
| 1.16 Site Management Safety Inspections | Fill out the weekly site-manager sheets | JE | rate 5, D — "not done" |
| 3.01 F10 — HSE Notification | F10 lapsed 6/5/25: contact Alison to renew, or confirm it's done | KLH | rate 0, C. Nigel 2 Sep: "contact Alison to resolve or ask if it has been done" |
| 5.03 Perimeter Fencing | Heras keeps coming away — fit the stored fence panels | JE | Nigel 2 Sep: "Of course, James Everitt will sort" |
| 8.01 Scaffolding | No scaff tags and no certificate — obtain both | (unassigned) | rate 5 but classed E; no owner on the sheet |
| 10.03 Fire Alarm Systems | Fire bell came off — put it back up | JE | rate 5, D |
| 11.01 Canteen Facilities | None on site | (unassigned) | rate 5, D — may be N/A for this site |
| 3.04 Fire Plan Layout | Owner KLH, class D, but rated 10 with no text | KLH | ask Katy what was meant |

**Blockers before the import lands on people:** neither **Katy-Louise Hicks** (`Katy-Louise.Hicks@JewelBB.co.uk`, needs `HealthSafetyOfficer`) nor **James Everitt** (`james.everitt@jewelps.co.uk` — a Jewel Property Serve address, needs `SiteManager`) has a portal login; `AssignedToEmail` needs them to exist. Admin creates both in the portal first.

## Not in this build (left in workflow 04 for later)
Mobilisation checklist and gate; scheduled inspections with overdue escalation; incident investigation; permits-to-work module; temporary works register; subcontractor RAMS/induction acceptance; automatic class penalties in the score (revisit with Katy once she's used the form).

## Open with Katy before build starts
- Confirm the scoring rule and whether class penalties should ever bite (decision 3 is "match the spreadsheet for now").
- What time-scale she'd expect per class if the portal proposed one.
- Serial numbering: hers is blank — the portal mints `HSA-0001` per project unless she has a company-wide sequence.
- 3.04 Fire Plan Layout and 11.01 Canteen — what she intended.

**Deploy order:** migration (new tables, no change to `HsRecord`) → build + tests locally (James) → deploy api + jpms → create Katy's and James Everitt's logins → run the By France seed → Katy reviews the audit page and its eight actions.
