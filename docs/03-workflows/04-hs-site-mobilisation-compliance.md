# Workflow 04 — H&S Site Mobilisation & Compliance

**Lifecycle stage:** 04 — the controlled readiness gate before site delivery starts (Workflow 06).
**Purpose:** Make site mobilisation a controlled, evidenced readiness gate; run the H&S framework as a connected engine (inspections, audits, incidents, near misses, corrective actions); separate H&SO ownership of the framework from Site Manager ownership of physical confirmation on site.
**Trigger:** Subcontractor procurement complete on workflow 03; site needs to mobilise.
**Frequency:** Per project at mobilisation; ongoing inspections / audits / incidents through the life of the site.
**Owner (target):** H&SO owns the framework, templates, audits, incident governance, corrective actions; Site Manager owns physical confirmation that controls are in place on site.
**Status:** Draft

---

## Why this is a first-class workflow

H&S can't live as a section inside site reporting. The framework, the audit regime, the inspection templates, the incident governance and the corrective-action loop need to be a connected system. The accountability split between H&SO (framework) and Site Manager (physical confirmation on site) needs to be reflected in JPMS, not implicit.

This is also the home of the **Inspections engine** (a generic capability that workflows 04, 06 and 08 all use) and the **Incidents / Observations / Corrective Actions engine** (cross-cutting through the life of the project).

---

## Current state

1. H&S evidence scattered across RAMsApp, SharePoint, paper toolbox-talk sheets, photos in WhatsApp.
2. Site mobilisation happens informally — no formal readiness gate; site can "start" without inductions, RAMS or welfare being signed off.
3. Inspections done on paper or on Dashpivot; no central evidence trail.
4. Incidents and near-misses sit outside the corrective-action loop.

---

## Target flow

1. **Project mobilisation gate** is opened when subcontractor procurement (03) reaches award; site cannot move to live delivery (06) until the gate clears.
2. **Mobilisation checklist** is populated from project + subcontractor data: welfare arrangements, signage, RAMS receipt + acceptance, inductions completed, permits in place, temporary works approved, fire arrangements, emergency arrangements, plant checks, scaffold / ladder inspections.
3. **H&SO supplies the framework** (templates, standards, what must be in place). **Site Manager confirms physically** that each item is on site.
4. **Inspections & audits** run on schedule. Each captures photos, signatures, timestamps. Overdue inspections escalate automatically.
5. **Observations / incidents / near-misses** logged from site (mobile-first). Each can become a corrective action with an owner, deadline and evidence-on-close.
6. **Toolbox talks**, inductions, permits to work, temporary works records all live as discrete records under the project.
7. **Subcontractor acceptance** of RAMS, induction and permits is captured digitally via the subcontractor portal.

---

## Cross-cutting modules introduced here

These engines are owned by this workflow but used by others:

- **Inspections engine** — template, schedule, instance, photo evidence, signature, timestamp, overdue escalation. Used by 04 (H&S), 06 (quality observations), 08 (snag inspections).
- **Observations / Incidents engine** — observation, near-miss, incident, investigation, corrective action. Used by 04 primarily; observations can also surface in 06.
- **Corrective Actions register** — owner, deadline, evidence-on-close. Cross-cutting across 04, 06, 08.

---

## JPMS functionality required

- Mobilisation checklist module per project (template + project instance).
- Inspections engine (templates + scheduled instances + photo / signature / timestamp).
- Audits engine (formal audits with findings, owners and close-out).
- Incidents / near-miss / observation register with investigation workflow.
- Corrective action register with owner, deadline, evidence-on-close, overdue escalation.
- Toolbox talks register with attendance.
- Inductions register with subcontractor acceptance via the portal.
- Permits-to-work module (issue, expiry, close).
- Temporary works register.
- "Golden thread" assembly for any item: every inspection, signature, timestamp and photo tied back to the project record.

---

## User stories

| ID | Role | Story | Status |
|---|---|---|---|
| US-04-01 | P06 H&SO | As an H&SO, I want to own a library of inspection and audit templates, so that the framework is consistent across projects. | Drafted |
| US-04-02 | P06 H&SO | As an H&SO, I want the mobilisation checklist template to be populated for a new project from project + subcontractor data, so that I'm not configuring it from scratch each time. | Drafted |
| US-04-03 | P05 Site Manager | As a Site Manager, I want to confirm each mobilisation-checklist item physically on site from my phone with a photo, so that the evidence is captured at the moment the control is in place. | Drafted |
| US-04-04 | P05 Site Manager | As a Site Manager, I want JPMS to block the move from mobilisation to live delivery (workflow 06) until every required mobilisation item is signed off, so that we don't start work before the controls are in place. | Drafted |
| US-04-05 | P06 H&SO | As an H&SO, I want scheduled inspections to fire automatically and escalate if overdue, so that nothing slips through the cracks. | Drafted |
| US-04-06 | P06 H&SO | As an H&SO, I want to capture a formal audit on tablet with findings, owners and close-out deadlines, so that audit follow-through is tracked. | Drafted |
| US-04-07 | P05 Site Manager | As a Site Manager, I want to raise an observation or near-miss from my phone in two taps with photo + voice note, so that I capture issues when I see them. | Drafted |
| US-04-08 | P06 H&SO | As an H&SO, I want to investigate an incident in-system with notes, witness statements and root-cause capture, so that the investigation is part of the audit trail. | Drafted |
| US-04-09 | P06 H&SO | As an H&SO, I want any observation, near-miss or incident to spawn a corrective action with owner, deadline and evidence-on-close, so that nothing dies in a notes field. | Drafted |
| US-04-10 | P10 Subcontractor | As a subcontractor, I want to accept RAMS, complete an induction and acknowledge permits-to-work through my portal before I start, so that my onboarding is recorded once rather than chased. | Drafted |
| US-04-11 | P05 Site Manager | As a Site Manager, I want to run a toolbox talk and capture attendance digitally (signatures + photo), so that the talk record is defensible. | Drafted |
| US-04-12 | P05 Site Manager | As a Site Manager, I want to issue a permit-to-work with expiry and close, so that high-risk activities are gated. | Drafted |
| US-04-13 | P06 H&SO | As an H&SO, I want a temporary works register with design, approval and inspection records, so that temporary works are controlled like permanent works. | Drafted |
| US-04-14 | P06 H&SO | As an H&SO, I want a "golden thread" assembly for any item — every inspection, signature, timestamp and photo tied to the project record — so that compliance evidence is defensible end-to-end. | Drafted |
| US-04-15 | P01 Director / MD | As a Director, I want a portfolio H&S view (open corrective actions by project, overdue inspections, recent incidents), so that I can spot risk concentration without asking. | Drafted |
| US-04-16 | P06 H&SO | As an H&SO, I want corrective actions to escalate on overdue, so that unresolved items reach the right person automatically. | Drafted |

---

## Integrations

- **HMRC CIS** — verification for subcontractor compliance (also used by workflow 03).
- **E-signature provider** (TBD) — RAMS acceptance, induction sign-off, audit findings sign-off.

---

## Acceptance criteria — "done looks like"

- Site cannot mobilise to live delivery without a clean mobilisation checklist.
- Every inspection / audit / incident / observation lives in the H&S engine, not on paper or in WhatsApp.
- Corrective actions are owned, dated and closed with evidence — no orphan items.
- Any compliance request (broker, insurer, client) can be answered from the golden thread in minutes.
- H&SO owns the framework; Site Manager confirms it on site; OCC files the paperwork. Roles are visibly distinct.

---

## Entities touched

`Project` · `Subcontractor` · `Mobilisation Checklist` · `Inspection Template` · `Inspection (instance)` · `Audit` · `Observation` · `Incident` · `Near Miss` · `Corrective Action` · `Toolbox Talk` · `Induction Record` · `Permit` · `Temporary Works` · `Compliance Document`

See [`/05-data-model/entities.md`](../05-data-model/entities.md).

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| P05 Site Manager | **Co-owner** — physical on-site confirmation; toolbox-talk delivery; observation / near-miss capture; permits-to-work; mobilisation sign-off |
| P06 H&SO | **Owner** — framework, templates, audits, incident investigation, corrective-action governance |
| P07 OCC | Contributor — files the resulting paperwork into the compliance register |
| P10 Subcontractor | Contributor — RAMS acceptance, induction, permit acknowledgement via portal |
| P01 Director / MD | Approver — annual H&S review; portfolio risk decisions |
| P03 PM | Contributor — paperwork side of H&S (RAMS issue admin, audit follow-up) |

See [`/05-data-model/permissions-matrix.md`](../05-data-model/permissions-matrix.md).

---

## Open questions

- [ ] Mobilisation gate strictness — hard block until 100% green, or allow Director override?
- [ ] Inspection template versioning — should JPMS retain the exact template version used at each instance?
- [ ] Subcontractor portal authentication — magic link per project, or persistent account?
- [ ] Toolbox-talk content — managed inside JPMS or linked from external library?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the H&SO and a Site Manager
- [ ] Mobilisation gate behaviour confirmed
- [ ] Inspections / audits engine confirmed
- [ ] Observation / incident / corrective-action loop confirmed
- [ ] Subcontractor portal flow confirmed with a real subcontractor
- [ ] Permissions confirmed
- [ ] Signed off
