# Workflow 01 — Drawing Receipt & Distribution

**Group:** Project lifecycle
**Purpose:** Receive new or revised drawings from architects/CAs, version-control them, distribute to relevant project members and site team.
**Trigger:** Drawing issued by architect, CA, or engineer (email or portal).
**Frequency:** As received — multiple times per week per active project.
**Owner (target):** JPMS (automated) with Project & Commercial Lead oversight on supersedure decisions.
**Current monthly hours:** ~15 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. PM receives drawing by email (Outlook).
2. PM saves PDF to SharePoint project folder, manually renamed for version.
3. PM uploads same drawing to Buildertrend.
4. PM prints copies for site and distributes by email to relevant subcontractors and site managers.
5. Old drawings are manually marked as superseded if remembered.

---

## Target flow (post-automation)

1. Drawing arrives in a monitored inbox or shared upload portal.
2. JPMS captures the drawing and auto-extracts revision/title from filename or metadata.
3. JPMS supersedes the previous version automatically; the old version is flagged as archive.
4. JPMS notifies the project team and assigned subcontractors with the new revision.
5. PM is alerted only if the system cannot confidently determine revision or supersedure.
6. The site app shows the current revision automatically; no email distribution needed.

---

## JPMS functionality required

- Drawing register per project with revision history.
- Auto-supersede logic with PM override.
- Email-inbox monitor and/or upload portal.
- Subscriber / notification model — project members opted-in by role.
- Mobile-accessible viewer for site (PDF, mark-up).
- Full audit trail of who has viewed which revision.

---

## Integrations & adjacent systems

- **SharePoint** (archive only after rollout).
- **Outlook** (inbound monitoring for the drawings inbox).
- **Bluebeam** (handoff to QS mark-up flow — see workflow 02).

See [`/do../05-data-model/integrations.md`](../05-data-model/integrations.md) for direction (read/write) and target status (keep / replace / archive).

---

## User stories

Status per story: **Drafted** · **In Review** · **Confirmed**

| ID | Role | Story | Status |
|---|---|---|---|
| US-01-01 | P08 Architect | As an architect, I want to issue a new or revised drawing into JPMS through a single channel (monitored inbox or upload portal), so that I don't have to chase the project team to confirm receipt. | Drafted |
| US-01-02 | JPMS (system) | As JPMS, I want to auto-extract revision and title from drawing filename or metadata on ingest, so that the project team doesn't manually rename and re-file. | Drafted |
| US-01-03 | JPMS (system) | As JPMS, I want to supersede the previous drawing revision automatically and flag the old one as archive, so that no-one can accidentally read the wrong version. | Drafted |
| US-01-04 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want JPMS to alert me only when it can't confidently determine revision or supersedure, so that my attention goes to the ambiguous cases instead of every drawing. | Drafted |
| US-01-05 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to override the auto-supersedure decision when needed, so that I stay in control of the drawing register. | Drafted |
| US-01-06 | P05 Site Team | As a site manager, I want the current drawing revision available on my phone for every assigned project, so that I never work from a superseded drawing. | Drafted |
| US-01-07 | P10 Subcontractor | As a subcontractor, I want to be notified when a drawing on my assigned work has been revised, so that I don't carry on against the old version. | Drafted |
| US-01-08 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to see who has viewed which drawing revision (audit trail), so that I can prove distribution if a dispute arises. | Drafted |

Covers spreadsheet row 6 (James Clark — PDF drawings from emails, save, upload, print).

---

## Acceptance criteria — "done looks like"

- PM no longer manually saves, renames or distributes drawings.
- Site team always sees the current revision in one place.
- Zero instances of work done from a superseded drawing.

---

## Entities touched

`Project` · `Drawing` · `Drawing Revision` · `Subcontractor` · `Inbox Message`

See [`/docs/data-models/entity-relationship.md`](../05-data-model/entities.md).

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Project & Commercial Lead | Owner of supersedure-override decisions |
| Site Team | Read current revision on mobile |
| Subcontractor | Read assigned drawings; notified on revision |
| Architect / CA (external) | Source — issues the drawing |
| JPMS (system) | Capture, supersede, notify |

See [`/docs/requirements/permission-matrix.md`](../05-data-model/permissions-matrix.md).

---

## Open questions

- [ ] Single drawings inbox or one per project?
- [ ] Tolerance for mis-detected revision — what's the fall-back when auto-extract fails?
- [ ] Retention policy on superseded revisions (forever, project lifetime, or N years post close-out)?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed (additions or removals)
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
