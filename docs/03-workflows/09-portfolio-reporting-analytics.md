# Workflow 09 — Portfolio Reporting & Analytics

**Lifecycle stage:** 09 — cross-cutting; reads from every other stage.
**Purpose:** Give Directors and the FD a portfolio-level view of risk, margin and performance: leading indicators across projects, cross-project trends, sales analytics from Workflow 00, and exception alerts that escalate when thresholds are crossed.
**Trigger:** Continuous — refreshes whenever upstream stages publish data.
**Frequency:** Always live; weekly Director review; monthly portfolio cycle.
**Owner (target):** Finance Director (operational owner); Directors / MD (executive audience).
**Status:** Draft

---

## Why this exists

Today, portfolio-level questions ("which projects are slipping?" "which subcontractors carry the most risk exposure?" "what's our win rate by lead source?") get answered by assembling spreadsheets from scratch. JPMS already holds every data point — this workflow is the surface that puts those answers one click away.

---

## What portfolio analytics surfaces

### Project delivery health
- Overdue RFIs by project / architect / age.
- Open variations awaiting client / Director approval, by value.
- Unvalued work done (work-in-progress not yet billed).
- Inspections missed (overdue against schedule from workflow 04).
- Defects backlog by project / trade / age.
- Programme slippage by project — predicted vs contracted PC date.

### Commercial health
- Project margin actual vs target, with trend.
- Margin leakage by project / client / estimator / package / trade / cause.
- Cost-code overruns by project.
- Aged debt (consumes data; debt itself is managed by accountancy downstream in Chaser HQ).
- Subcontractor exposure — financial concentration risk by subcontractor across projects.

### H&S health
- Open corrective actions by project, age and severity.
- Overdue inspections by project.
- Recent incidents / near-misses by project.
- Subcontractor H&S record across projects.

### Sales / CRM health (from Workflow 00)
- Pipeline value by stage and weighted probability.
- Win rate by source, project type, value band.
- Bid-hit rate by estimator.
- Average lead-to-won time.
- Lost-lead reasons clustered.

### Exception alerts
- Director-level escalation when defined thresholds are crossed (e.g. project margin drops below X, RFI ageing past Y days, corrective action overdue, retention release pending past close-out).

---

## Target flow

1. **Always-live dashboards** assembled from upstream stages (00, 03, 04, 05, 06, 07, 08).
2. **Configurable thresholds** per indicator. When crossed, an exception fires to the FD and (depending on severity) to a Director.
3. **Snapshot per Claim Period** retained so historical comparison is possible.
4. **Drill-down to source** — every number on the dashboard clicks through to the underlying project / subcontractor / lead.

---

## JPMS functionality required

- Dashboard engine consuming events from workflows 00, 03, 04, 05, 06, 07, 08.
- Threshold / alerting layer.
- Snapshot retention (per Claim Period).
- Director-level and FD-level views (different scope, different intervention controls).
- Drill-down navigation from any aggregate down to the source record.

---

## User stories

| ID | Role | Story | Status |
|---|---|---|---|
| US-09-01 | P01 Director / MD | As a Director, I want a single portfolio dashboard that surfaces project health, commercial health, H&S health and sales health, so that I can see the whole business in one place. | Drafted |
| US-09-02 | P02 Finance Director | As an FD, I want overdue RFIs, open high-value variations and unvalued work-in-progress across the portfolio, so that I can intervene before they become cashflow problems. | Drafted |
| US-09-03 | P01 Director / MD | As a Director, I want a margin actual vs target view per project with trend, so that margin erosion is visible early. | Drafted |
| US-09-04 | P01 Director / MD | As a Director, I want subcontractor exposure (financial concentration risk) across projects, so that I can spot dependency on a single subcontractor. | Drafted |
| US-09-05 | P06 H&SO | As an H&SO, I want open corrective actions by project / age / severity, so that systemic H&S issues surface across the portfolio. | Drafted |
| US-09-06 | P02 Finance Director | As an FD, I want recurring defects clustered by trade and subcontractor, so that quality issues drive procurement decisions. | Drafted |
| US-09-07 | P01 Director / MD | As a Director, I want pipeline value by stage with weighted probability (from Workflow 00), so that I see expected revenue without asking sales. | Drafted |
| US-09-08 | P01 Director / MD | As a Director, I want win rate by lead source / project type / value band, so that marketing and sales decisions are data-informed. | Drafted |
| US-09-09 | P04 QS / Estimator | As a QS, I want bid-hit rate by estimator and lost-lead reason clusters, so that I can improve my own win rate. | Drafted |
| US-09-10 | P02 Finance Director | As an FD, I want to configure thresholds per indicator with exception alerts that route to me or to a Director, so that we manage by exception. | Drafted |
| US-09-11 | P01 Director / MD | As a Director, I want every aggregate number to drill into the source project / subcontractor / lead, so that I can investigate without leaving the dashboard. | Drafted |
| US-09-12 | P02 Finance Director | As an FD, I want a portfolio snapshot retained per Claim Period, so that historical comparison and trend analysis are possible. | Drafted |

---

## Acceptance criteria — "done looks like"

- The Director portfolio view answers "where is the business?" in under 30 seconds.
- Margin erosion, RFI ageing, defects backlog and H&S exception surface automatically rather than being assembled on request.
- Exception alerts reach the right person without anyone having to look for them.
- Every dashboard number drills to its source.

---

## Entities touched

`Portfolio Snapshot` · `Leading Indicator` · `Threshold` · `Exception Alert` · plus read-access on entities from every upstream workflow.

See [`/05-data-model/entities.md`](../05-data-model/entities.md).

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| P01 Director / MD | **Audience** — primary consumer; configures escalation thresholds |
| P02 Finance Director | **Owner** — operational owner of the dashboards and thresholds |
| P03 PM | Read — project-scoped slice |
| P04 QS / Estimator | Read — commercial and bid-hit slice |
| P06 H&SO | Read — H&S slice |

See [`/05-data-model/permissions-matrix.md`](../05-data-model/permissions-matrix.md).

---

## Open questions

- [ ] Threshold defaults — start with conservative defaults, tune later?
- [ ] Snapshot cadence — per Claim Period, weekly, both?
- [ ] How does the Director scoped view differ from the FD view — fewer controls, same data?
- [ ] External reporting — does anything need to leave JPMS for board packs / lender reporting?

---

## Confirmation checklist

- [ ] Walked through end-to-end with a Director and the FD
- [ ] Thresholds and alerting confirmed
- [ ] Drill-through confirmed for every aggregate
- [ ] Snapshot retention confirmed
- [ ] Permissions confirmed
- [ ] Signed off
