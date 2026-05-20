# Workflow 04 — Variations, RFIs & Delays (in-flight changes)

**Group:** Project lifecycle
**Purpose:** Track and formalise the three main change events on an active project: variations, RFIs, and delay notices.
**Trigger:** Architect instruction, client change, site issue, design clash, or programme impact identified.
**Frequency:** Weekly per active project; total cycle ~15 events/month across portfolio.
**Owner (target):** Project & Commercial Lead (review/approve); JPMS for capture, register and issuance.
**Current monthly hours:** ~25 h/month.
**Status:** Draft
**Last reviewed:** —
**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

---

## Current state

1. Three separate Excel logs maintained per project: variation register, RFI register, delay notices.
2. **Variation:** PM identifies, estimates impact, prices, emails for approval, updates Excel.
3. **RFI:** PM identifies gap on drawing, drafts question in Excel, emails architect, chases.
4. **NoD (Notice of Delay):** PM identifies impact, drafts a Word template, issues by email, updates Excel.
5. All three connect loosely to programme and valuation but require manual reconciliation.

---

## Target flow (post-automation)

1. All three events raised through a single "project change" entry point in JPMS.
2. System classifies the event type and routes to the right template/workflow.
3. **Variation:** priced against the rate library, attached to a BoQ line, approval requested in-system.
4. **RFI:** question goes to architect via JPMS portal or email-with-link; reply attaches automatically.
5. **Delay notice:** programme impact captured, formal letter auto-drafted from data, PM reviews and issues.
6. All three update programme and valuation automatically once approved (no manual rekey).

---

## JPMS functionality required

- Project change register (unified).
- Variation pricing against the BoQ rate library.
- RFI workflow with architect portal / email-link.
- NoD letter generator from project data.
- Approval routing (PM → client/CA).
- Automatic feed into programme (workflow 05) and valuation (workflow 05) once approved.

---

## Integrations & adjacent systems

- **Email** (external CAs/architects).
- **Programme module** (internal — workflow 05).
- **Valuation module** (internal — workflow 05).

---

## Acceptance criteria — "done looks like"

- Variations, RFIs and NoDs all live in JPMS, not standalone Excel logs.
- Approved variations automatically appear on the next valuation.
- Architect responses arrive in JPMS, not in an inbox.

---

## Entities touched

`Project` · `BoQ Line Item` · `Variation` · `RFI` · `NoD` · `Programme Task` · `Valuation`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Project & Commercial Lead | Owner — raises, prices, routes for approval |
| Architect / CA (external) | Approver — RFI replies and variation sign-off |
| Subcontractor (external) | Source — many RFIs originate from site |
| Finance Director | Read — visibility on commercial impact |

---

## Open questions

- [ ] Approval thresholds — does the MD/Director approve variations above a value?
- [ ] RFI chase cadence — how many days before auto-reminder, and to whom?
- [ ] NoD format — single template, or per-contract?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
