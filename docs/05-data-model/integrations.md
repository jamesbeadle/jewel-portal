# Integrations & System Landscape

How JPMS relates to the wider system landscape at JBB. Three sections:

1. **Inputs to JPMS** — systems that feed data *into* JPMS or that JPMS calls out to.
2. **What JPMS replaces** — tools currently in use for project management that JPMS supersedes.
3. **Downstream consumers** — accountancy and back-office tools that draw on JPMS data. These are *not* JPMS integrations; they're listed once for completeness so it's clear where JPMS data ends up after it leaves the system.

**Status:** Draft.

---

## 1. Inputs to JPMS

The short list of systems JPMS actually integrates with. Anything not on this list is either replaced by JPMS or downstream of it.

| System | Direction | Used for | Status |
|---|---|---|---|
| **Google OAuth** | → JPMS | Sign-in: one of three options offered to invited users. | Required (auth foundation) |
| **Microsoft OAuth** | → JPMS | Sign-in: one of three options offered to invited users. | Required (auth foundation) |
| **Email + password** | → JPMS | Sign-in: one of three options. JPMS itself is the identity provider for this path. | Required (auth foundation) |
| **Bluebeam** | → JPMS | Take-off quantities imported into the BoQ (workflow 02). Mark-up handoff to QS work. | Phase 2 |
| **Monitored email inboxes (Outlook / IMAP)** | → JPMS | Inbound channel for drawings (workflow 01) and architect replies on RFIs (workflow 04). | Phase 2 |
| **HMRC CIS verification** | ↔ | Subcontractor compliance gate before award (workflow 08). | Phase 2 |

Auth identifies a user by their email address. A user who first signs in with Google for `alice@example.com` can equally use the email+password path with the same address — the JPMS account is the email, not the OAuth provider chosen on first access. Admins and Project & Commercial Leads invite users by email; the user picks a sign-in method on first access.

---

## 2. What JPMS replaces (for project management)

Tools currently in use for project management work. After JPMS rollout these are decommissioned or kept read-only for historical data.

| System (today) | Today's role | Replaced by, in JPMS |
|---|---|---|
| **MS Project** | Programme tracking; updated manually by the PM. | Programme module (workflow 05) — Gantt-style view tied to BoQ line items, updated automatically from site reporting. |
| **Buildertrend** | Drawing distribution; work-order contracts. | Workflow 01 (drawing register with revisions) and workflow 03 (work-order generation on award). |
| **Planyard** | Real-time cost control marketed as a replacement for Excel CVR workbooks: cost estimating, budgets, committed costs, CVR / margin tracking, CIS and VAT calculations. Currently piloted at JBB as the replacement for the old Excel CVR. | **Workflow 07 delivers the full CVR surface plus the three improvements over the Planyard-style pilot called out by the JBB QS lead:** (1) traceable Forecast Final Cost components — Cost Incurred / Cost Committed / QS Accruals / Prelim Forecast / Cost to Complete, not a black box; (2) Prelims and EOTs visible against tender separately with a weeks-ahead/behind tracker; (3) per-package variation margin alongside the central Variations Register, from the same underlying data. Plus the cashflow forecast and the Programme Valuation Report. **Planyard subscription not required.** Also covers the work-order side previously handled in Planyard (workflow 03). |
| **Monday.com** | Subcontractor directory and attendance tracking. | Workflow 08 (subcontractor master record + compliance register) and workflow 06 (attendance check-in inside the site app). |
| **Dashpivot** | Site capture (photos, attendance, snags). | Workflow 06 — site app captures progress, photos, attendance and snags against BoQ sections. |
| **RAMsApp** | RAMS drafting. | Workflow 08 — RAMS template engine populated from project + subcontractor data and issued from JPMS. |
| **WhatsApp (operational use)** | Site photos and ad-hoc requests. | Workflow 06 — site capture inside the JPMS site app. |
| **Excel — BoQ workbook** | Standalone priced BoQ per project. | Workflow 02 — BoQ is a JPMS record with hierarchical line items, units, rates, version history. |
| **Excel — programme tracker** | Manual programme tracking. | Workflow 05 — programme module in JPMS. |
| **Excel — valuation sheet** | Monthly valuation per project, built by hand. | Workflow 05 — auto-generated Programme Valuation Report per Claim Period. |
| **Excel — variations / RFI / NoD logs** | Three separate logs per project. | Workflow 04 — unified project change register. |
| **Excel — cashflow tracker** | FD rebuilds weekly. | Workflow 10 — live cashflow forecast built from JPMS project data alone. |
| **Excel — subcontractor attendance tracker** | Per-project Excel / calendar. | Workflow 06 — attendance check-in via QR or geofence in the site app. |
| **Excel — timesheet allocation tracker** | Manual cost-code allocation per period. | Workflow 09 — cost-code allocation enforced inline at timesheet entry with the budget hard-block rule. |
| **Excel — settlement / VAT workbook** | Manual at project close. | Workflow 08 — settlement workspace with auto-generated zero-rated VAT analysis and in-system client agreement. |
| **Excel — CVR workbook** (e.g. JBB's "By France" CVR — both the old version and the new pilot) | Project Cost-Value Reconciliation rebuilt monthly per project, plus QS Accruals and Prelim Forecast on separate sheets. | Workflow 07 — JPMS CVR is live, computed from the same project data as the Programme Valuation Report and the cashflow forecast. Fixes the three issues James called out on the JBB pilot: forecast traceability, Prelims/EOTs against tender, per-package variation margin. |
| **Word — RAMS template** | Per-project drafted RAMS. | Workflow 08. |
| **Word — neighbour letter / contract drafts** | Drafted by hand. | Not in JPMS scope — these are office-admin tasks; the drafting tools stay where they are. |

---

## 3. Downstream consumers (out of JPMS scope)

Systems the JBB accountancy and operations teams use after JPMS data leaves the project management surface. **None of these are JPMS integrations** — JPMS publishes data; these tools consume it through whatever means the accountancy team prefers (Xero export, CSV, copy-paste, future API).

| System | What it does | What it consumes from JPMS |
|---|---|---|
| **Xero** | Accountancy ledger: AP, AR, VAT postings, retention transactions. | Work orders for AP matching; approved valuations for AR invoicing; settlement / VAT analysis at project close. |
| **Dext** | Invoice OCR capture into Xero. | Nothing direct — feeds Xero on the supplier-invoice side. |
| **Brightpay** | Internal staff payroll. | Nothing — internal staff payroll is unrelated to JPMS. (Subcontractors are paid via AP, not payroll.) |
| **Chaser HQ** | Chases overdue AR invoices in Xero. | Nothing — operates on Xero data. |
| **Online banking** | Payment execution. | Nothing — payment runs are an accountancy task. |
| **HMRC reporting** | VAT and CIS returns. | Nothing — accountancy generates these from Xero. |

If any of these systems later need a direct integration into JPMS (rather than a downstream consumption pattern), they'd be promoted into Section 1.

---

## Phase-1 integration shortlist

Just the inputs that must be live for JPMS phase 1:

1. **OAuth sign-in** — Google, Microsoft, email/password (auth foundation).
2. **Monitored email inboxes** — drawings (workflow 01) and RFI replies (workflow 04).
3. **HMRC CIS verification** — subcontractor compliance gate (workflow 08).
4. **Bluebeam** — take-off import (workflow 02; can land slightly later if needed).

Everything else (Section 2 replacements and Section 3 downstream consumers) is unaffected by JPMS phase 1 and unfolds naturally over time.
