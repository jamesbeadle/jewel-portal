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

1. Each JPMS project is linked to a dedicated **Bluebeam Studio Project** on project creation — Studio Projects is the canonical drawing store.
2. Architect / CA uploads a new or revised drawing into the project's Studio Project (the channel they already use).
3. Bluebeam fires a webhook to JPMS — JPMS pulls the latest revision via the **Studio Projects API**, auto-extracts revision/title from filename or metadata.
4. JPMS supersedes the previous Drawing Revision automatically; the old version is flagged as archive.
5. JPMS notifies the project team and assigned subcontractors with the new revision.
6. PM is alerted only if the system cannot confidently determine revision or supersedure.
7. Site app shows the current revision automatically; QS opens the drawing in Bluebeam Revu (same Studio Project) for take-off — no duplicate file moves.
8. Fall-back: if an architect emails a drawing instead of uploading to Studio, a monitored inbox channel ingests it and JPMS pushes it into the linked Studio Project for the canonical record.

**The QS never re-uploads a drawing into JPMS.** The act of saving a new revision into the Studio Project IS the upload.

---

## JPMS functionality required

- Drawing register per project, sourced from a linked Bluebeam Studio Project.
- Studio Projects API integration — list files, fetch latest revisions, resolve revision metadata.
- Studio webhook subscription — receive file-added and file-revised events per Studio Project.
- Project-creation step that provisions (or links to) the matching Studio Project.
- Auto-supersede logic with PM override.
- Subscriber / notification model — project members opted-in by role.
- Mobile-accessible viewer for site (PDF).
- Full audit trail of who has viewed which revision.
- Fall-back inbox monitor for architects not on Studio (drawings re-published into the Studio Project after ingest).

---

## Integrations & adjacent systems

- **Bluebeam Studio Projects** (primary input — canonical drawing store; see workflow 02 for the take-off side).
- **Outlook / IMAP** (fall-back inbox channel for architects not using Studio).
- **SharePoint** (archive only after rollout).

See [`/docs/05-data-model/integrations.md`](../05-data-model/integrations.md) for direction (read/write) and target status (keep / replace / archive).

---

## User stories

Status per story: **Drafted** · **In Review** · **Confirmed**

| ID | Role | Story | Status |
|---|---|---|---|
| US-01-01 | P08 Architect | As an architect, I want to upload a new or revised drawing into the project's Bluebeam Studio Project (the channel I already use), so that the JBB project team picks it up automatically without me chasing receipt. | Drafted |
| US-01-02 | JPMS (system) | As JPMS, I want to auto-extract revision and title from drawing filename or metadata on ingest, so that the project team doesn't manually rename and re-file. | Drafted |
| US-01-03 | JPMS (system) | As JPMS, I want to supersede the previous drawing revision automatically and flag the old one as archive, so that no-one can accidentally read the wrong version. | Drafted |
| US-01-04 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want JPMS to alert me only when it can't confidently determine revision or supersedure, so that my attention goes to the ambiguous cases instead of every drawing. | Drafted |
| US-01-05 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to override the auto-supersedure decision when needed, so that I stay in control of the drawing register. | Drafted |
| US-01-06 | P05 Site Team | As a site manager, I want the current drawing revision available on my phone for every assigned project, so that I never work from a superseded drawing. | Drafted |
| US-01-07 | P10 Subcontractor | As a subcontractor, I want to be notified when a drawing on my assigned work has been revised, so that I don't carry on against the old version. | Drafted |
| US-01-08 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to see who has viewed which drawing revision (audit trail), so that I can prove distribution if a dispute arises. | Drafted |
| US-01-09 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want each JPMS project linked to a Bluebeam Studio Project on creation, so that the drawing store is established up front and the QS / site team are looking at the same Studio Project from day one. | Drafted |
| US-01-10 | JPMS (system) | As JPMS, I want to subscribe to Bluebeam Studio webhooks for each project's linked Studio Project, so that new or revised drawings land in the JPMS drawing register without a PM upload step. | Drafted |
| US-01-11 | P04 Quantity Surveyor | As a QS, I want to open the project's drawings in Bluebeam Revu directly from the JPMS drawing register (one click into the linked Studio Project), so that I do take-off against the same canonical drawing the rest of the team is reading. | Drafted |
| US-01-12 | JPMS (system) | As JPMS, when a drawing is emailed in via the fall-back inbox, I want to re-publish it into the project's linked Studio Project automatically, so that the Studio Project remains the canonical store regardless of how the architect issued the drawing. | Drafted |

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

- [ ] One Bluebeam Studio Project per JPMS project, or a single Studio Project per Organisation (BB / PS / PFP) with folder-per-project inside?
- [ ] Studio Project provisioning — does JPMS create the Studio Project on project creation via API, or is it linked to an existing one the QS has already set up?
- [ ] Architects without Bluebeam — do we issue them a Studio Sessions invite (Sessions Roundtrip, phase 2), or stay on the email-inbox fall-back?
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
