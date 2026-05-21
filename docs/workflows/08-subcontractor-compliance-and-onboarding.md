# Workflow 08 — Subcontractor Compliance & Onboarding

**Group:** Supplier & subcontractor management
**Purpose:** Maintain up-to-date insurance, certifications, tickets, CIS status, and RAMS for every subcontractor working on a project.
**Trigger:** New subcontractor added; expiry approaching; project requiring RAMS; CIS check needed.
**Frequency:** Continuous; ~6 tasks/month per active subcontractor tracking cycle.
**Owner (target):** Office & Compliance Coordinator (oversight); JPMS for tracking and reminders.
**Current monthly hours:** ~10 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Subcontractor details kept in Monday.com, SharePoint folders, and an Excel tracker.
2. Insurance / certificate expiry chased manually 30 days before expiry.
3. RAMS drafted in RAMsApp per project, sent to client for approval.
4. CIS verified manually through the HMRC portal.

---

## Target flow (post-automation)

1. Single subcontractor record in JPMS with all compliance docs attached.
2. Expiry dates tracked centrally; automated reminders 60/30/7 days before.
3. Subcontractor has a portal to upload new docs themselves.
4. RAMS auto-drafted from project + subcontractor data, reviewed and issued from JPMS.
5. CIS verification integrated with HMRC; status held against the record.

---

## JPMS functionality required

- Subcontractor master record.
- Document register with expiry tracking and reminders.
- Subcontractor self-service portal.
- RAMS template engine populated from project + subcontractor data.
- HMRC CIS integration — or status field with audit if no integration.

---

## Integrations & adjacent systems

- **HMRC CIS service**.
- **RAMsApp** (legacy migration only).
- **Client portals** where required (Dwellant / Vantify).

---

## User stories

| ID | Role | Story | Status |
|---|---|---|---|
| US-08-01 | P04 Office & Compliance Coordinator | As an Office & Compliance Coordinator, I want one master record per subcontractor with all compliance documents attached, so that I'm not chasing the same data across Monday, SharePoint and Excel. | Drafted |
| US-08-02 | P04 Office & Compliance Coordinator | As an Office & Compliance Coordinator, I want JPMS to send expiry reminders to the subcontractor 60 / 30 / 7 days before each document expires, so that I'm not the human reminder service. | Drafted |
| US-08-03 | P02 Subcontractor | As a subcontractor, I want to log into a self-service portal and see exactly which of my documents are current, expiring soon, expired or missing, so that I know what to upload. | Drafted |
| US-08-04 | P02 Subcontractor | As a subcontractor, I want to upload a renewed document with drag-and-drop and have JPMS OCR the expiry date and pre-fill the form, so that I'm not retyping data the system can read. | Drafted |
| US-08-05 | P02 Subcontractor | As a subcontractor, I want to refresh my CIS status with one click that calls HMRC, so that I don't have to manage that step manually. | Drafted |
| US-08-06 | P04 Office & Compliance Coordinator | As an Office & Compliance Coordinator, I want to be notified when a subcontractor uploads a document that needs my sign-off, so that I review only the items that need review. | Drafted |
| US-08-07 | P04 Office & Compliance Coordinator | As an Office & Compliance Coordinator, I want to draft RAMS from a template auto-populated with project + subcontractor data, so that producing RAMS takes minutes rather than redrafting each time. | Drafted |
| US-08-08 | P04 Office & Compliance Coordinator | As an Office & Compliance Coordinator, I want to send RAMS to the client for approval through JPMS, so that the approval is captured in-system rather than in an inbox. | Drafted |
| US-08-09 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want JPMS to block the workflow 03 award path if the chosen subcontractor's compliance is expired or missing, so that we can't accidentally award work to a non-compliant subcontractor. | Drafted |
| US-08-10 | P06 Finance Director | As a Finance Director, I want CIS status visible against every subcontractor so the accountancy team has the information they need at the point of payment downstream. | Drafted |

Covers spreadsheet rows 36 (send RAMS to client), 37 (draft RAMS), 48 (monitor expiry dates), 49 (maintain subcontractor details).

---

## Acceptance criteria — "done looks like"

- No subcontractor works on a project with expired documents.
- RAMS produced in minutes from existing data, not redrafted each time.
- CIS status is current and visible to the accountancy team at the point of payment downstream.

---

## Entities touched

`Subcontractor` · `Compliance Document` · `RAMS` · `Project` · `Work Order`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Office & Compliance Coordinator | Owner — oversight, gating |
| Subcontractor (external) | Contributor — self-service uploads |
| Project & Commercial Lead | Read — checks status before award |
| Finance Director | Read — gates payment |

---

## Open questions

- [ ] HMRC CIS — API available, or scraping/manual?
- [ ] Document expiry grace — soft block (warn) or hard block (no work) at expiry?
- [ ] RAMS approval — client-side approval needed before subcontractor mobilises?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
