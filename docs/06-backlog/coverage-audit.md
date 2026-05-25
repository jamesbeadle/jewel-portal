# Coverage audit — task analysis spreadsheet → user stories

**Status:** Draft — for sign-off with the business owner.
**Source:** `Jewel Task Analysis (1).xlsx` → sheet *Consolidated Tasks Analysis*.
**Method:** Every row coloured **red (can be automated)** or **yellow (human-in-loop)** for a JBB-relevant staff member is mapped to a covering user story (or flagged as a gap). Rows coloured **blue (not related to JBB)** are excluded from the audit but recorded so it's clear they were considered.

## Headline numbers

109 JBB-relevant rows reviewed (47 automate + 62 human-in-loop) across six staff members.

| Status | Count | Meaning |
|---|---|---|
| **Covered by existing Phase 1 stories** | 32 | An existing user story (or a tight set of stories) delivers the row inside Phase 1. No further action needed. |
| **Closed by new Phase 1 stories proposed below** | 9 | The row is not fully covered by an existing Phase 1 story (often because the existing story assumes a Phase 2 integration). One of `US-NEW-01` … `US-NEW-07` closes it. |
| **Out of JPMS scope by design** | 68 | Accountancy (AP/AR/payroll/Dext/CIS submission), HR, IT admin, office facilities, fleet, marketing, materials/long-lead procurement. Excluded from JPMS by `00-business-context/delivery-principles.md` rule #2 and `05-data-model/integrations.md`. Logged here so nothing slips through unnoticed. |

By staff member: James Clark 11 in scope + 1 out of scope. Chris Reeves 17 in scope + 2 out of scope. Jeremy Ferendinos 4 in scope + 22 out of scope. Katie-Louise Hicks 6 in scope + 29 out of scope. Sarah Collins 3 in scope + 11 out of scope. Sofia 0 in scope + 3 out of scope.

**Seven new Phase 1 stories close nine spreadsheet rows.** Mapping:

| Spreadsheet row | Staff | New story closing the row |
|---|---|---|
| 12 | James Clark | `US-NEW-01` (manual drawing upload) + `US-NEW-04` (architect drawing issue audit record) |
| 23 | Chris Reeves | `US-NEW-02` (direct BoQ entry) + `US-NEW-03` (BoQ bulk import) |
| 24 | Chris Reeves | `US-NEW-02` + `US-NEW-03` |
| 25 | Chris Reeves | `US-NEW-06` (rate staleness flag — replaces the Phase-2 AI suggestion story for v1) |
| 34 | Chris Reeves | `US-NEW-02` + `US-NEW-03` |
| 37 | Chris Reeves | `US-NEW-02` + `US-NEW-03` |
| 78 | Sarah Collins | `US-NEW-05` (H&SO assigns next TBT topic) |
| 88 | Katie-Louise Hicks | `US-NEW-07` (proactive project-status update card to client portal) |
| 89 | Katie-Louise Hicks | `US-NEW-07` |

---

## How to read this audit

For each row: the spreadsheet row number (so you can find it in the workbook), the task as written by the staff member, the covering user story (or stories), and any commentary on coverage. **Where a row maps to a brand-new proposed story, the ID is `US-NEW-NN` and the proposal is listed in section *New stories proposed* at the end.**

---

## James Clark — Project Manager (12 tasks; 11 in JPMS scope, 1 out of scope)

| Row | Task | Covered by | Coverage |
|---|---|---|---|
| 10 | Check emails every morning | The whole platform — inboxes are replaced by the project change register (US-05-01), the RFI portal (US-05-06), the subcontractor portal (US-03-15), and the per-role dashboards (US-09-01). | **Covered** by the *capture once, flow forward* principle: if every inbound thread lives in the right project record, the morning inbox triage disappears. |
| 11 | Update to-do list from previous day | Per-role dashboard surfaces items needing attention; portfolio exceptions queue (US-07-39, US-09-10). | **Covered** — items needing attention are derived from project data, not held on a parallel list. |
| 12 | PDF drawings from emails, save, upload, print | New stories `US-NEW-01` (manual drawing upload) + `US-NEW-04` (architect drawing issue audit record). Existing Phase 1 stories provide revision control once the file is in (US-01-02, US-01-03, US-01-05, US-01-06, US-01-08). Phase 2 adds Studio Projects auto-ingest (US-01-01, US-01-09, US-01-10, US-01-12). | **Closed by new story.** The existing Phase 1 stories handle revision control but assume the file is already in the register; the new stories provide the upload path. Auto-ingest from Bluebeam fully eliminates the task at Phase 2. |
| 13 | Update valuation Excel sheet | PVR auto-assembly (US-07-03, US-07-04, US-07-05); historic series (US-07-08). | **Covered**. |
| 14 | Create variations in Excel | Variation entry and pricing against BoQ (US-05-02); central register and per-package CVR view (US-07-32, US-07-33). | **Covered**. |
| 15 | RFIs on Excel weekly to architects | Single change entry classified to RFI (US-05-01); architect portal reply (US-05-06); auto-chase (US-05-08). | **Covered**. |
| 16 | Notice of Delays weekly/monthly | NoD letter auto-drafted from project data (US-05-09); approval and issue. | **Covered**. |
| 17 | Update Excel programmes / MS Project | Programme as Gantt tied to BoQ (US-07-01); auto-updated from approved variations (US-05-12) and site % (US-06-04). | **Covered**. |
| 18 | Email subcontractor quotes / bid packages | Bid package builder (US-03-01); subcontractor invite (US-03-03); inline pricing return (US-03-05). | **Covered**. |
| 19 | Work order contracts in Buildertrend / Planyard | Auto-generated work order on award (US-03-10); central WO register feeds CVR (US-07-23, US-07-25). | **Covered**. |
| 20 | Contractor report (photos to PDF, email to CAs) | Site app capture (US-06-04, US-06-05); auto-assembled weekly/monthly report (US-06-09); PM narrative + approve (US-06-10); architect/client live dashboard (US-06-11). | **Covered**. |
| 21 | Process supplier invoices and receipts in Dext | **Out of JPMS scope by design.** JPMS publishes work-order data (US-03-10) so the accountancy team can match in Xero downstream; the supplier-invoice → Dext → Xero flow stays in the accountancy stack. | **Out of scope (intentional)** — see `delivery-principles.md` rule #2 and `integrations.md` section 3. |

---

## Chris Reeves — Estimator / QS (19 tasks; 17 in JPMS scope, 2 out of scope)

| Row | Task | Covered by | Coverage |
|---|---|---|---|
| 22 | Review Tender Drawings & Scope of Works | Drawings in JPMS register accessible to QS (US-01-06); P2 adds Bluebeam Revu deep link (US-01-11). | **Covered** — drawings are in JPMS day one; the *review activity* sits with the QS regardless. |
| 23 | Complete Take-off for new projects | **P1:** direct BoQ entry by QS (proposed new story `US-NEW-02`). **P2:** Bluebeam CSV import (US-02-03) and then Markups API direct (US-02-04). | **Partial → new story needed for P1.** Today's stories assume Bluebeam at v1. |
| 24 | Use Bluebeam to work out quants of materials and labour | Same as row 23 — direct BoQ entry P1, Bluebeam paths P2. | **Partial → new story needed for P1.** |
| 25 | Research Updated Rates | Rate library with last-used rates per trade/supplier (US-02-05); new story `US-NEW-06` (rate staleness flag for Phase 1); AI rate suggestion (US-02-06) moves to Phase 2. | **Closed by new story** — `US-NEW-06` is the Phase 1 equivalent of the Phase 2 AI suggestion. |
| 26 | Site meetings | Walk-round capture on mobile (US-02-09); site report / programme capture (US-06-01 → 08). | **Covered**. |
| 27 | Create VOR's | Variation entry and pricing (US-05-02); per-package CVR roll-up (US-07-32, US-07-33). | **Covered**. |
| 28 | Send out Bid packages | Bid package builder (US-03-01, US-03-03). | **Covered**. |
| 29 | Create tender document folders for relevant Trade bid packages | Auto-create folder structure on issue (US-03-02). | **Covered**. |
| 30 | Compare submitted tenders from subcontractors | Side-by-side comparison view (US-03-08). | **Covered**. |
| 31 | Create Defect Schedules for completed product | Defect register on project (US-08-01); site-app defect raise (US-08-02); auto-assign by trade/BoQ section (US-08-03). | **Covered**. |
| 32 | New project walk rounds | Walk-round capture on mobile (US-02-09). | **Covered**. |
| 33 | Retender current projects to confirm accuracy of quants & rates | Re-tender comparison view (US-02-08). | **Covered**. |
| 34 | Use quants formatted from Bluebeam to create a completed take-off | Same Bluebeam path as 23/24. | **Partial → new story needed for P1.** |
| 35 | Review updated tender drawings and accurately identify any changes | Drawing register supersedure + revision audit (US-01-03, US-01-05, US-01-08); QS-side drawing access (US-01-06). | **Covered** for the "spot the change" outcome. JPMS shows the current and previous revision; a per-revision diff is not in scope today and is not requested in the spreadsheet. |
| 36 | Review documents produced by AI | No JPMS story — this is a one-off review of an AI artefact rather than a recurring workflow. | **Out of scope (one-off review activity, not a workflow JPMS owns).** |
| 37 | Review M&E drawings to create BoQ's | M&E discipline tag on BoQ lines (US-02-07); same take-off path as rows 23/24. | **Partial → new story needed for P1.** |
| 38 | Site meetings with approved subcontractors | Covered as for row 26. | **Covered**. |
| 39 | Completing AI audit template & weekly reports | Auto-assembled weekly/monthly report (US-06-09); narrative review (US-06-10). | **Covered**. |
| 40 | Reviewing Workflows of JBB | This is the work *we are doing now* (analysing the business). Not a recurring JPMS workflow. | **Out of scope (project-level activity, not a recurring task JPMS runs).** |

---

## Jeremy Ferendinos — Finance Director (26 tasks; 4 in JPMS scope, 22 out of scope by design)

The FD persona explicitly says: *"The FD's day job — AP, AR, payroll, inbox triage, statement reconciliation — runs in Xero / Dext / Brightpay / Chaser HQ. That work is **not** a JPMS workflow."* That's the framing for this section.

### In JPMS scope

| Row | Task | Covered by | Coverage |
|---|---|---|---|
| 49 | Maintain short-term cashflow tracking for JBB and JPS | 13-week rolling cashflow dashboard (US-07-36); cross-entity flag (US-07-38); items-needing-attention queue (US-07-39); drill-to-source (US-07-40); stress-test (US-07-41); snapshot per Claim Period (US-07-42). | **Covered** — this is the **primary driver of JPMS** (delivery-principles #6 and the README headline). |
| 51 | Track in-house costs and cross-charge costs between entities/projects | Cross-entity flag at source (US-07-38); cost-code allocation (US-07-14); audit log (US-07-18). | **Covered**. |
| 54 | Track supplier work orders and match invoices to WO / project info | JPMS publishes the WO register cleanly for the accountancy team to match in Xero (US-07-22 same pattern; US-03-10 generates the WO with project + cost-code reference). | **Covered** for the *JPMS-side publish*. The *match in Xero* itself is the accountancy team's job downstream and stays in Xero. |
| 56 | Verify CIS status of subcontractors and maintain CIS records | CIS status visible on every subcontractor (US-03-22); reminders before expiry (US-03-14); P2 adds one-click HMRC refresh (US-03-17). | **Covered** for P1 (manual CIS status + reminders); HMRC API direct is P2. |

### Out of JPMS scope (explicit, by design)

| Row | Task | Why out of scope |
|---|---|---|
| 41 | Raise Sales invoices for Jewel BB | AR is in Xero. JPMS publishes the approved PVR (US-07-10); Xero raises the invoice. |
| 42 | Reconcile supplier statements for JBB / JPS / JPFP | Bookkeeping reconciliation in Xero / Dext. |
| 43 | Read accounts Inbox in BB/PS/PFP | Accountancy inbox, not project comms. |
| 44 | Maintain shared accounts inbox triage | Accountancy admin. |
| 45 | Chasing outstanding invoices JBB / JPS / JPFP | Chaser HQ (downstream of Xero). |
| 46 | Chase outstanding sales invoices and agree payment dates | Chaser HQ. |
| 47 | Run supplier payment reviews and payment runs | Xero + online banking. |
| 48 | Payroll JBB / JPS / JPFP | Brightpay. |
| 50 | Chase missing receipts and supporting documents from staff | Dext + accountancy admin. |
| 52 | Dext allocation in JBB / JPS / JPFP | Dext. |
| 53 | Process Dext invoice allocation and approval workflow | Dext + Xero. |
| 55 | Track subcontractor corrected invoices and chase amendments | Accountancy admin. |
| 57 | Prepare and send CIS statements / submissions | HMRC reporting (Xero output). |
| 58 | Attend management / operations / support meetings and follow-ups | General meeting admin. |
| 59 | Review insurance documents, renewals, schedules and claims-related information | Insurance / office admin. |
| 60 | Maintain accreditation and compliance document reviews | OCC owns the subcontractor-side; corporate accreditation is office admin. |
| 61 | Support HR administration on recruitment, contracts and onboarding | HR. |
| 62 | Set up new users and coordinate starter IT access | IT admin. |
| 63 | Resolve day-to-day IT support issues | IT admin. |
| 64 | Maintain SharePoint / Teams / permissions structure | IT admin. |
| 65 | Update and maintain group IT architecture / systems map | IT admin. |
| 66 | Review security, licensing and system admin across M365 tenants | IT admin. |

If the business owner *wants* any of these inside JPMS later, raise a Phase 3+ scope conversation. The current scope statement keeps JPMS focused on project lifecycle.

---

## Sofia — Marketing (3 tasks; 0 in JPMS scope)

| Row | Task | Why out of scope |
|---|---|---|
| 67 | Create new social posts for LinkedIn and Facebook | Marketing automation — listed in `06-backlog/phase-2.md`. |
| 68 | Creating and updating Jewel BB brochure | Marketing collateral. |
| 69 | Categorising/organising folders | Document filing — not a JPMS workflow. |

---

## Sarah Collins — H&S / compliance admin (14 tasks; 2 in JPMS scope)

### In JPMS scope

| Row | Task | Covered by | Coverage |
|---|---|---|---|
| 71 | Sent RAMS to client for approval | Send RAMS to client through JPMS for approval (US-03-20). | **Covered**. |
| 72 | Drafted RAMS for new won project | Draft RAMS from template auto-populated with project + subcontractor data (US-03-19). | **Covered**. |
| 78 | Assigned this week's TBT on professional etiquette and handling abuse to operatives | New story `US-NEW-05` (H&SO assigns next TBT topic from the H&SO-managed library). Existing US-04-11 captures the TBT happening; this gap is the *assignment* step. | **Closed by new story.** |

### Out of JPMS scope

| Row | Task | Why out of scope |
|---|---|---|
| 70 | Send test emails from info inbox to check signature changes | IT / email admin. |
| 73 | Forwarded "Your Home, Your Safety" bulletin to directors | One-off comms forwarding. |
| 74 | Reviewed "Your Home, Your Safety" bulletin | Reading / research. |
| 75 | Completed insurance renewal form | Annual insurance admin. |
| 76 | Checked info inbox for new enquiries | Replaced by CRM lead capture (US-00-01) where the inbound is a JBB lead; general info-inbox triage is admin. |
| 77 | Replied to Nigel on AI ethical/legal process chain | Internal AI policy work. |
| ~~78~~ | reclassified — listed in *In JPMS scope* above as a gap closed by `US-NEW-05` |
| 79 | Contacted operatives to confirm uniform/PPE requirements | Internal staff comms. |
| 80 | AI meeting | One-off meeting. |
| 81 | Updated amendments to Staff Handbook | HR. |
| 82 | Reviewed update emails from Akeva on courses, recalls, H&S case law | H&S subscription content. |
| 83 | Reviewed Jeremy's IT/AI/Cybersecurity policy, GDPR booklet | IT/policy admin. |

Note: row 78 was originally on the spreadsheet's *out of scope* radar, but the *assignment* step (which TBT topic, when, on which projects) is a JPMS workflow that the existing US-04-11 doesn't capture. Hence the new story `US-NEW-05` proposed below.

---

## Katie-Louise Hicks — Office & Compliance Coordinator (35 tasks; 6 in JPMS scope)

### In JPMS scope

| Row | Task | Covered by | Coverage |
|---|---|---|---|
| 84 | Organise and monitor subcontractor attendance for jobs / put into Nigel's calendar | Subcontractor check-in via app (US-06-02) and manual override (US-06-03). Attendance is auditable without the Excel tracker. The *push-to-Nigel's-calendar* step disappears because the data lives in JPMS instead. | **Covered**. |
| 85 | Raise works orders (WOs) for subcontractors, chase progress and save documentation | Auto-generated WO on award (US-03-10); full tender history per project (US-03-12); open WO tail at close-out (US-08-10). | **Covered**. |
| 86 | Keep an eye on expiry dates for subcontractor insurance, certs and tickets | Auto-reminders at 60/30/7 days (US-03-14); doc status visible to OCC + subcontractor (US-03-15, US-03-18). | **Covered**. |
| 87 | Maintain subcontractor details and documents in the correct folders/systems | One master record per subcontractor with compliance docs attached (US-03-13). | **Covered**. |
| 88 | Liaise with clients on any enquiries from reactive calls/emails | New story `US-NEW-07` (proactive project-status update card to client portal) + existing client portal scoped views (US-06-11, US-07-45) reduce inbound enquiries. | **Closed by new story** — reactive calls themselves stay with humans; the *what's-the-status?* categories disappear once the portal carries the status. |
| 89 | Provide updates to clients on access, progress and any issues where needed | New story `US-NEW-07` (proactive update card) + client live dashboard (US-06-11, US-07-45). | **Closed by new story.** |

### Out of JPMS scope

| Rows | Topic | Why out of scope |
|---|---|---|
| 90 | Site deliveries coordination | Materials/long-lead procurement is `06-backlog/phase-2.md` (Procore-style). Phase 1 stays out. |
| 91 | Printing documents | Office facilities. |
| 92 | Event planning | Office admin. |
| 93 | Drafting professional emails on behalf of directors | Comms drafting, not a JPMS workflow. |
| 94 | Sending policies out to be signed, adding headed papers | Policy admin (e-signature provider is in `phase-2.md`). |
| 95 | Social Media Scheduling | Marketing — `phase-2.md`. |
| 96 | New starter onboarding | HR. |
| 97 | Make sure everyone is on the correct platforms/systems | IT admin. |
| 98 | Remind the team about upcoming deliveries, collections, key dates | General team admin. |
| 99 | Contact suppliers for quotes, updates, placing orders | Materials/long-lead procurement — `phase-2.md`. |
| 100 | Organise and place equipment and materials orders | Same. |
| 101 | Check equipment orders on arrival | Same. |
| 102 | Handle handyman / office Amazon receipts and upload to Dext | Accountancy / Dext. |
| 103 | Answer reactive calls and emails and route to the right person | Reception / admin. |
| 104 | Draft and sort neighbour letters | Office admin. |
| 105 | Organise folder names in OneDrive/SharePoint | IT / office admin. |
| 106 | Maintain Staff & HR, Design/Brand shared folders | HR / IT admin. |
| 107 | Contact Regus monthly to check on post | Office facilities. |
| 108 | Change printer ink and paper | Office facilities. |
| 109 | Maintain stock levels for office supplies | Office facilities. |
| 110 | Arrange birthday cards | Office admin. |
| 111 | Oversee van fleet administration | Fleet admin. |
| 112 | Sort company van fines | Fleet admin. |
| 113 | Support the team in using systems properly | IT admin. |
| 114 | Tidy and move documents saved in the wrong place | IT / office admin. |
| 115 | Posting letters | Office admin. |
| 116 | Supporting Nigel and directors with anything needed | General admin. |
| 117 | Calls specific company to get a company's specific email/info required | Reception / admin. |
| 118 | Help maintain company design and brand assets folders | Marketing / IT admin. |

---

## Gaps — new stories proposed

Seven genuine gaps surface from the audit. Adding these to the relevant workflow file before the business-owner walkthrough.

| ID | Workflow | Proposed story | Why it's needed |
|---|---|---|---|
| `US-NEW-01` | 01 Drawing & Doc Control | As a Project & Commercial Lead, I want to upload a drawing PDF into the JPMS drawing register directly (drag-and-drop, with revision and title fields), so that the team has a single canonical revision without depending on Bluebeam in Phase 1. | Today every workflow-01 story assumes the drawing arrives from a Bluebeam Studio Project. P1 needs a manual-upload path; this is the most important gap. |
| `US-NEW-02` | 02 Tender & BoQ | As a QS, I want to create BoQ line items directly inside JPMS (line, unit, quantity, rate, cost code, discipline), so that the BoQ is a JPMS record from day one without depending on Bluebeam in Phase 1. | Spreadsheet rows 23, 24, 34, 37 (Chris Reeves' take-off work) all currently rely on the Bluebeam path. P1 needs the manual / direct-entry alternative. |
| `US-NEW-03` | 02 Tender & BoQ | As a QS, I want to bulk-import BoQ line items from a generic CSV/Excel template into JPMS, so that an existing BoQ workbook can be migrated into the system in one step without the Bluebeam tool-set. | Same logic as `US-NEW-02` — a structured import path the QS can use day one. |
| `US-NEW-04` | 01 Drawing & Doc Control | As a Project & Commercial Lead, I want to record the architect's drawing issue (date, revision, source) against the JPMS drawing register so the issue audit trail exists in Phase 1, even when the PDF was emailed in rather than fetched from Studio. | Spreadsheet row 12 (James Clark — "PDF drawings from emails, save, upload, print") needs the issue side of the audit captured in P1. |
| `US-NEW-05` | 04 H&S Mobilisation | As an H&SO, I want to assign the upcoming Toolbox Talk topic from the H&SO-managed library to the relevant projects, so that TBTs are programmed rather than ad-hoc. | Spreadsheet row 78 (Sarah Collins — "Assigned this week's TBT on professional etiquette and handling abuse to operatives") is not covered by existing US-04-11, which only captures the TBT happening. |
| `US-NEW-06` | 02 Tender & BoQ | As a QS, I want JPMS to flag rate-library entries that have not been priced for more than N days, so that stale rates surface for review without depending on AI suggestion in Phase 1. | Spreadsheet row 25 (Chris Reeves — "Research Updated Rates"); the existing US-02-06 ("AI suggests updated rates") is moved to P2 in the phase proposal, so a non-AI staleness signal is needed for P1. |
| `US-NEW-07` | 06 Site Delivery | As a Project Manager, I want to publish a project-status update card to the client portal in one action (text + photos + current % per BoQ section), so that "what's happening on my project this week?" is answered without a bespoke email. | Spreadsheet rows 88 + 89 (Katie-Louise Hicks — client status updates). Closes the *partial* coverage on those rows. |

These new stories slot into the existing workflow files in Stage 2.

---

## What this audit is not

It is not a re-validation of the user stories themselves — every existing story is taken at face value. It is a coverage check between today's manual work and tomorrow's JPMS. Stories that already exist may still need refinement during the walkthrough; that is the walkthrough's job, not this audit's.
