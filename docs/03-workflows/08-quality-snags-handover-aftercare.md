# Workflow 08 — Project Close-Out & Defects

**Group:** Project lifecycle
**Purpose:** Manage the snagging, defect resolution, and formal close-out process at the end of a project.
**Trigger:** Practical completion or formal handover.
**Frequency:** Per project — typically several active close-outs in parallel.
**Owner (target):** Project & Commercial Lead (sign-off); JPMS for defect register and workflow.
**Current monthly hours:** ~5 h/month (likely under-captured).
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Defect schedule created in Excel from site inspection.
2. Items emailed to relevant subcontractors for resolution.
3. Status chased manually by email/phone.
4. Final sign-off captured by email or in a notebook.

---

## Target flow (post-automation)

1. Defect register raised in JPMS against the project, with photos and BoQ references.
2. Each defect auto-assigned to the responsible subcontractor with a deadline.
3. Subcontractor updates status via JPMS portal or app; photos as evidence.
4. Project Lead signs off each defect; final sign-off triggers retention release.
5. Close-out pack (warranties, O&Ms, defect record) assembled by JPMS.

---

## JPMS functionality required

- Defect register module per project.
- Subcontractor assignment with deadline tracking.
- Photo evidence and resolution audit trail.
- Close-out pack generator (warranties, O&Ms, sign-offs).
- Retention release trigger → finance.

---

## Integrations & adjacent systems

- **Subcontractor portal** (workflow 03 — same portal).
- **AP / retention release** (workflow 07).
- **Document library** (close-out pack output).

---

## User stories

| ID | Role | Story | Status |
|---|---|---|---|
| US-08-01 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to raise a defect register on the project at Practical Completion with photos and BoQ references on each item, so that the snag list isn't trapped in an Excel sheet. | Drafted |
| US-08-02 | P05 Site Team | As a site manager, I want to add a defect from the site app during the inspection walk, so that it's captured the moment I see it. | Drafted |
| US-08-03 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want JPMS to auto-assign each defect to the responsible subcontractor based on the trade / BoQ section with a deadline, so that I'm not emailing subcontractors one by one. | Drafted |
| US-08-04 | P10 Subcontractor | As a subcontractor, I want to update the status of a defect assigned to me through the portal with photo evidence of the fix, so that I can clear my list without phone calls. | Drafted |
| US-08-05 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to sign off each defect (or reject the subcontractor's evidence with a comment), so that resolution is auditable and not "we said it was done". | Drafted |
| US-08-06 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want JPMS to assemble the close-out pack (warranties, O&Ms, defect record, final sign-offs) when the last defect is signed off, so that pack assembly isn't a separate manual job. | Drafted |
| US-08-07 | P02 Finance Director | As a Finance Director, I want the retention release trigger to fire automatically when close-out is signed off, so that retention isn't held longer than necessary because nobody remembered to ask. | Drafted |
| US-08-08 | P08 Architect | As an architect / client, I want to receive the close-out pack through the portal with all warranties and O&Ms in one place, so that I have everything I need to operate the building. | Drafted |
| US-08-09 | P06 Finance Director | As a Finance Director, I want JPMS to open a settlement workspace on the project automatically when Practical Completion is declared, so that I don't have to assemble it from scratch. | Drafted |
| US-08-10 | P06 Finance Director | As a Finance Director, I want an open-items dashboard combining unapproved timesheets (09), zero-budget allocations, open WO tail (03), outstanding valuation items (05) in one view, so that nothing in commercial close-out lives only in someone's email. | Drafted |
| US-08-11 | P06 Finance Director | As a Finance Director, I want inline resolution actions per open item (approve, re-allocate, write off, amend), so that I can clear the list from one surface. | Drafted |
| US-08-12 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to sign off the commercial items at close (final valuation, variation tail, WO closure), so that I'm explicitly accountable for the commercial close. | Drafted |
| US-08-13 | P06 Finance Director | As a Finance Director, I want JPMS to produce a draft zero-rated VAT analysis from contract metadata + the settled cost ledger, identifying which BoQ items / cost codes qualify for zero-rating, so that I'm reviewing rather than building the analysis. | Drafted |
| US-08-14 | P06 Finance Director | As a Finance Director, I want to adjust the draft VAT analysis before it goes to the client, so that I can apply judgement on edge cases. | Drafted |
| US-08-15 | P01 Architect | As an architect / client, I want to receive the zero-rated VAT analysis through the portal and confirm agreement (or comment) in-system, so that the agreement is captured cleanly and not in an email chain. | Drafted |
| US-08-16 | P06 Finance Director | As a Finance Director, when the client disputes the VAT analysis, I want disputes routed back to me for revision, so that revision rounds are tracked rather than scattered across emails. | Drafted |
| US-08-17 | P07 Directors / MD | As a Director, I want to sign off the final VAT outcome with the client, so that the agreed position has the right authority behind it. | Drafted |
| US-08-18 | P06 Finance Director | As a Finance Director, when all open items are settled and VAT is agreed, I want JPMS to produce the final settlement record as an audit-grade summary, so that the closed project has a defensible single source of truth. | Drafted |
| US-08-19 | JPMS (system) | As JPMS, I want the settlement record to publish a retention-release trigger for the accountancy team to action in Xero, so that retention release happens on settlement completion rather than as a separate ask. | Drafted |
| US-08-20 | P06 Finance Director | As a Finance Director, I want closed projects archived to a read-only state with director-override on writes, so that the project record stays defensible after close. | Drafted |

Covers spreadsheet row 19 (Create Defect Schedules for completed product).

---

## Acceptance criteria — "done looks like"

- Defects are tracked to closure with photo evidence.
- Close-out pack is assembled from JPMS, not built by hand.
- Retention release happens on close-out sign-off, not by separate request.

---

## Entities touched

`Project` · `Defect` · `Subcontractor` · `Work Order` · `BoQ Line Item` · `Compliance Document`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Project & Commercial Lead | Owner — sign-off on each defect and the close-out pack |
| Subcontractor (external) | Contributor — updates status + photo evidence |
| Finance Director | Approver — retention release |
| Architect / CA (external) | Read — close-out pack delivery |

---

## Open questions

- [ ] Retention percentage — global or per work-order?
- [ ] Warranties — where stored after close-out (JPMS vs SharePoint archive)?
- [ ] Defect-period clock — starts at PC, or at handover?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
