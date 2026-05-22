# Workflow 03 — Subcontractor Procurement (Bid → Award)

**Group:** Project lifecycle
**Purpose:** From issuing a bid package to awarding a work order: managing the tender process for subcontractors.
**Trigger:** BoQ ready, new work package identified, or variation requiring subcontractor pricing.
**Frequency:** Weekly while a project is in pre-construction or experiencing variations.
**Owner (target):** Project & Commercial Lead (sign-off); JPMS for data and document handling.
**Current monthly hours:** ~35 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Bid package compiled manually from BoQ extracts + drawings + scope notes.
2. Tender document folders created manually in SharePoint per trade.
3. Subcontractors emailed bid packages with instructions and deadline.
4. Returned quotes arrive by email; assembled into an Excel comparison sheet manually.
5. Award decision made; work order contract drafted in Buildertrend / Planyard.
6. Subcontractor onboarded into systems separately (Monday, RAMsApp, compliance docs).

---

## Target flow (post-automation)

1. Bid package auto-assembled from JPMS: BoQ items for the trade + drawings + standard T&Cs.
2. Subcontractors invited via JPMS portal (or email-with-secure-link if they aren't users).
3. Quotes returned directly into JPMS comparison view; no manual transcription.
4. Side-by-side comparison auto-generated; Project Lead reviews and awards.
5. On award, a work order is auto-generated with agreed scope, price, and T&Cs.
6. Subcontractor compliance check (workflow 03) runs automatically as part of award.

---

## JPMS functionality required

- Bid package builder (BoQ extract + drawings + T&Cs templates).
- Subcontractor directory with contact details and trade tags.
- Tender portal — or email-with-link — for non-system users.
- Quote return mechanism (form or upload).
- Comparison and award workflow with approval routing.
- Work order generation from approved quote.
- Tender history per project (who quoted, who won, at what price).

---

## Integrations & adjacent systems

- **Email** (for subcontractors not on the system).
- **Xero** (for payment terms reference).
- **Compliance workflow 03** (gated by compliance status).

---

## User stories

| ID | Role | Story | Status |
|---|---|---|---|
| US-03-01 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to assemble a bid package in one action from BoQ lines, drawings and standard T&Cs, so that I'm not stitching together SharePoint / Outlook / Excel each time. | Drafted |
| US-03-02 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want JPMS to auto-create the tender document folder structure when a bid package is issued, so that document storage is consistent across projects. | Drafted |
| US-03-03 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to invite subcontractors to a bid package via the JPMS portal or a secure email-with-link, so that non-system subcontractors can still participate without extra tooling. | Drafted |
| US-03-04 | P10 Subcontractor | As a subcontractor, I want to open a bid package from a single secure link and see the trade-relevant BoQ, drawings and deadline on one screen, so that I can decide whether to bid quickly. | Drafted |
| US-03-05 | P10 Subcontractor | As a subcontractor, I want to enter my prices inline against each BoQ line and submit my quote without learning a new tool, so that my response lands directly in JPMS with no manual transcription. | Drafted |
| US-03-06 | P10 Subcontractor | As a subcontractor, I want to attach qualifications, assumptions, exclusions and lead times to my quote, so that my commercial position is clear before award. | Drafted |
| US-03-07 | P10 Subcontractor | As a subcontractor, I want to decline a bid package in one click with a reason, so that I'm not chased for a quote I'm not bidding on. | Drafted |
| US-03-08 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want a side-by-side comparison of returned quotes, so that I can make an award decision without rebuilding the comparison in Excel. | Drafted |
| US-03-09 | P07 Office & Compliance Coordinator | As an Office & Compliance Coordinator, I want JPMS to block the award path if the chosen subcontractor's compliance documents are expired, so that work isn't given to non-compliant subcontractors. | Drafted |
| US-03-10 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want JPMS to auto-generate the work order when I award a bid, so that the contract artefact exists immediately and downstream AP can match against it. | Drafted |
| US-03-11 | P01 Directors / MD | As a Director, I want to approve any award above the agreed threshold before it's issued, so that high-value commitments don't slip out without sign-off. | Drafted |
| US-03-12 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to see the full tender history per project (who quoted, who won, at what price), so that I can defend award decisions and inform future tenders. | Drafted |
| US-03-13 | P04 Office & Compliance Coordinator | As an Office & Compliance Coordinator, I want one master record per subcontractor with all compliance documents attached, so that I'm not chasing the same data across Monday, SharePoint and Excel. | Drafted |
| US-03-14 | P04 Office & Compliance Coordinator | As an Office & Compliance Coordinator, I want JPMS to send expiry reminders to the subcontractor 60 / 30 / 7 days before each document expires, so that I'm not the human reminder service. | Drafted |
| US-03-15 | P02 Subcontractor | As a subcontractor, I want to log into a self-service portal and see exactly which of my documents are current, expiring soon, expired or missing, so that I know what to upload. | Drafted |
| US-03-16 | P02 Subcontractor | As a subcontractor, I want to upload a renewed document with drag-and-drop and have JPMS OCR the expiry date and pre-fill the form, so that I'm not retyping data the system can read. | Drafted |
| US-03-17 | P02 Subcontractor | As a subcontractor, I want to refresh my CIS status with one click that calls HMRC, so that I don't have to manage that step manually. | Drafted |
| US-03-18 | P04 Office & Compliance Coordinator | As an Office & Compliance Coordinator, I want to be notified when a subcontractor uploads a document that needs my sign-off, so that I review only the items that need review. | Drafted |
| US-03-19 | P04 Office & Compliance Coordinator | As an Office & Compliance Coordinator, I want to draft RAMS from a template auto-populated with project + subcontractor data, so that producing RAMS takes minutes rather than redrafting each time. | Drafted |
| US-03-20 | P04 Office & Compliance Coordinator | As an Office & Compliance Coordinator, I want to send RAMS to the client for approval through JPMS, so that the approval is captured in-system rather than in an inbox. | Drafted |
| US-03-21 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want JPMS to block the workflow 03 award path if the chosen subcontractor's compliance is expired or missing, so that we can't accidentally award work to a non-compliant subcontractor. | Drafted |
| US-03-22 | P06 Finance Director | As a Finance Director, I want CIS status visible against every subcontractor so the accountancy team has the information they need at the point of payment downstream. | Drafted |

Covers spreadsheet rows 9 (email subcontractor quotes / bid packages), 10 (work-order contracts), 16 (send bid packages), 17 (tender document folders), 18 (compare submitted tenders), 47 (raise WOs).

---

## Acceptance criteria — "done looks like"

- Bid package is built in one action, not assembled across SharePoint / Outlook / Excel.
- Subcontractor quotes appear in JPMS without manual transcription.
- Awarded work orders are published cleanly so the accountancy team can match invoices against them in Xero (downstream).

---

## Entities touched

`Project` · `BoQ Line Item` · `Bid Package` · `Subcontractor` · `Quote` · `Work Order` · `Compliance Document`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Project & Commercial Lead | Owner — award decision |
| Office & Compliance Coordinator | Contributor — supplier directory upkeep |
| Subcontractor (external) | Source — submits quotes |
| Finance Director | Read — visibility on awarded value |

---

## Open questions

- [ ] Approval threshold for award sign-off — is FD/Director sign-off required above a value?
- [ ] How are tied bids resolved — pure price, or weighted scorecard?
- [ ] Standard T&Cs — single template or per-trade variants?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
