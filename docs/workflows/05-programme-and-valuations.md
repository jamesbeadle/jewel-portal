# Workflow 05 — Programme & Valuations (project control)

**Group:** Project lifecycle
**Purpose:** Maintain the project programme and produce monthly valuations against contract and approved variations.
**Trigger:** Monthly valuation cycle, significant variation, or programme impact event.
**Frequency:** Monthly; updates as needed in between.
**Owner (target):** Project & Commercial Lead (review and issuance); JPMS for the calculation and assembly.
**Current monthly hours:** ~10 h/month.
**Status:** Draft
**Last reviewed:** —
**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

---

## Current state

1. PM updates MS Project programme manually based on site reports and judgement.
2. PM updates Excel valuation sheet line-by-line: contract sum, variations to date, work in progress, percentages.
3. Reconciliation between programme and valuation is manual.
4. Issued to CA/client as PDF/Excel attachment by email.

---

## Target flow (post-automation)

1. Programme lives in JPMS with tasks/phases tied to BoQ line items.
2. Site reporting (workflow 06) updates progress percentages automatically.
3. Valuation auto-generated each cycle: contract + variations + current %.
4. Project Lead reviews, applies judgement, approves, and issues from JPMS.
5. Client/CA receives the valuation through a portal or styled PDF.

---

## JPMS functionality required

- Programme module with Gantt-style view.
- BoQ-to-programme linkage so progress flows through.
- Valuation generator with variation roll-up.
- Approval and issuance workflow.
- Historic valuation series per project.

---

## Integrations & adjacent systems

- **Site reporting** (workflow 06).
- **Variations** (workflow 04).
- **AR invoicing** (workflow 10).

---

## Acceptance criteria — "done looks like"

- Monthly valuation takes minutes to review, not hours to rebuild.
- Programme reflects site reality without manual re-entry.
- Valuation feeds directly into the sales-invoice draft (workflow 10).

---

## Entities touched

`Project` · `BoQ Line Item` · `Programme Task` · `Valuation` · `Variation` · `Site Report`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Project & Commercial Lead | Owner — reviews, approves, issues |
| Site Team | Source — % progress per BoQ section |
| Finance Director | Read — feeds AR (workflow 10) |
| Architect / CA (external) | Recipient — receives issued valuation |

---

## Open questions

- [ ] MS Project import — migration only, or ongoing read?
- [ ] Valuation cadence — uniform monthly, or per-contract?
- [ ] Retention handling — applied at valuation or at AR invoice (workflow 10)?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
