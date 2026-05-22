# Workflow 06 — Site Reporting & Progress

**Group:** Project lifecycle
**Purpose:** Capture what is happening on site — progress, photos, issues, attendance — and turn it into client reports and live project status.
**Trigger:** Daily site activity; weekly/monthly reporting cycle; CA or client request.
**Frequency:** Daily capture; weekly/monthly formal reports.
**Owner (target):** Site team (capture); JPMS (assembly); Project Lead (review/issue).
**Current monthly hours:** ~25 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Site team photos to WhatsApp groups + their own phones.
2. PM collates photos, drafts narrative in Word/PowerPoint.
3. PM exports to PDF, emails to CA/client.
4. Subcontractor attendance tracked in a separate Excel / Dashpivot / calendar.
5. Information lives across WhatsApp, Photos, SharePoint and inboxes.

---

## Target flow (post-automation)

1. Site app captures progress (% per BoQ section), photos, notes, issues.
2. Subcontractors check in via the app (or QR/geofence) for attendance.
3. JPMS assembles weekly/monthly progress report automatically from captured data.
4. Project Lead reviews narrative, adds commentary, approves, issues from JPMS.
5. Client/CA can also view a live dashboard (selected fields) at any time.

---

## JPMS functionality required

- Site mobile app (photo, note, % capture against BoQ sections).
- Attendance check-in mechanism.
- Automated report assembly with narrative template.
- Issue / snag log capture from site.
- Client/CA dashboard (read-only, scoped).

---

## Integrations & adjacent systems

- **Mobile devices** (PWA / native).
- **WhatsApp** (one-way for legacy users initially).
- **Onetrace** where relevant.

---

## User stories

| ID | Role | Story | Status |
|---|---|---|---|
| US-06-01 | P05 Site Team | As a site manager, I want to open the JPMS site app on my phone, see today's project list, and tap into the active one, so that I can start capturing within two taps of opening the app. | Drafted |
| US-06-02 | P10 Subcontractor | As a subcontractor on site, I want to check in via QR scan or geofence on arrival, so that attendance is recorded without paper or chasing. | Drafted |
| US-06-03 | P05 Site Team | As a site manager, I want a manual attendance override with attestation, so that I can record someone who's clearly on site but forgot to check in. | Drafted |
| US-06-04 | P05 Site Team | As a site manager, I want to update progress per BoQ section by sliding a % indicator, with previous % and a suggested today % visible, so that progress capture is a two-tap action rather than a form to fill in. | Drafted |
| US-06-05 | P05 Site Team | As a site manager, I want photos taken in-app to be auto-tagged with project, BoQ section, time and GPS, so that nothing important ends up untagged in someone's camera roll. | Drafted |
| US-06-06 | P05 Site Team | As a site manager, I want to import photos from my phone's camera roll as a fallback, so that pictures taken outside the app aren't lost. | Drafted |
| US-06-07 | P05 Site Team | As a site manager, I want to raise a snag with photo + voice note + auto-assigned trade in two taps, so that capture matches the speed at which problems appear on site. | Drafted |
| US-06-08 | P05 Site Team | As a site manager, I want the app to work offline and sync on reconnect with an X-items-pending banner, so that poor signal doesn't lose entries. | Drafted |
| US-06-09 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want JPMS to assemble the weekly/monthly progress report automatically from captured data with a narrative template ready to edit, so that report assembly is review, not creation. | Drafted |
| US-06-10 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to add narrative commentary and approve the report before it's issued, so that the client gets the right framing. | Drafted |
| US-06-11 | P08 Architect | As an architect / CA, I want a live, scoped project dashboard so I can see progress and photos any time, so that I'm not chasing the project team for updates. | Drafted |
| US-06-12 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to re-tag a mis-tagged photo without losing it, so that mistakes from site are easy to fix from the office. | Drafted |

Covers spreadsheet rows 11 (contractor report — photos to PDF, email to CAs), 46 (organise & monitor subcontractor attendance) and 47-partial (chase progress).

---

## Acceptance criteria — "done looks like"

- Weekly site reports take minutes of review, not hours of assembly.
- Site photos live against the project, not in someone's WhatsApp.
- Subcontractor attendance is auditable without an Excel tracker.

---

## Entities touched

`Project` · `BoQ Line Item` · `Site Report` · `Defect` · `Subcontractor` · `Timesheet`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Site Team | Owner of capture |
| Subcontractor | Contributor — attendance + photos |
| Project & Commercial Lead | Approver — adds narrative, issues report |
| Architect / CA (external) | Read — live dashboard, scoped |

---

## Open questions

- [ ] Offline capture — required for poor-signal sites?
- [ ] Geofence vs QR for attendance — which is acceptable to subcontractors?
- [ ] Live dashboard scoping — which fields are exposed externally?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
