# Building Control tab — design spec

**Status:** Draft for director review (pre-build)
**Author:** Cowork (for Nigel Reilly)
**Date:** 27 August 2026
**Source artefacts:** SharePoint "Building Control" libraries for By France (Leas Green, Chislehurst BR7 6HD) and 17a Abbot Road, Guildford GU1 3TA

---

## 1. What we're building

A new **Building Control** row in the Project sidebar folder at `/projects/{project}/building-control` — the last major project-record area the portal doesn't yet cover. Building control is the statutory sign-off trail for a build: the notice/application that opens the case with a building control body, the staged site inspections as work proceeds (foundations, steel, drainage, insulation & fire lining, …), the inspector's site reports and photo evidence for each stage, and the completion certificate that closes the case at handover.

Today this lives in per-project SharePoint folders — one folder per inspection stage holding phone photos, the inspector's `.msg` emails and site-report PDFs, plus an approval folder with the initial notice, acknowledgement, planning decision and completion documents. It works, but nothing tracks **state**: nothing says which inspection is booked, which passed, which report never arrived, or whether the completion certificate is in hand — and the correspondence is filed by hand.

The portal version makes each inspection a **first-class record** with a status, a date, its documents and photos, and its own correspondence read live from the mailbox by tag — exactly the shape Requests, Defects and Tender Enquiries already have.

---

## 2. How building control runs today (from the SharePoint evidence)

- **One case per project with a building control body**, under one reference. The two example projects show the two regimes we deal with: a **local authority / partner authority** application (`BC2415406DOMFPB` at By France) and a **registered building control approver (private) initial notice** (`25-129527 Initial Notice (April 2024)` at Abbot Road, with its Dutyholder Notification alongside — the post-BSA paperwork).
- **Case-level documents:** initial notice or application, acknowledgement letter, planning permission + decision notice (kept with the case for the inspector's reference), the "who is our building control contact" email, notice of completion, completion certificates.
- **A sequence of inspection stages, different per project.** By France ran ~9 numbered stages (Foundations → Steel Columns → Block & Beam → Drainage → DPC & Insulation → Super Structure & Roof Framing → Insulation & Fire Lining → GF Insulation & Fire Lining); Abbot Road ran 4 (Ground Beam Reinforcement → Pad Foundation → Superstructure & Steel Frame, dated 01.04.26). Stage lists are similar but never identical — the model must not hard-code them.
- **Per stage:** site photos (phone uploads, GUID filenames), the inspector's emails (booking + follow-up, e.g. "…BC2415406DOMFPB - Foundations Inspection 1"), and sometimes a formal **Site Inspection Report** PDF from the body.
- **Folder names carry dates by hand** ("Superstructure & Steel Frame - 01.04.26") — i.e. the booked/inspected date is data people want, currently smuggled into a folder name.

---

## 3. Mapping to the current system

This is almost entirely assembly of proven patterns — the closest analogues are the Defect register and Tender Enquiry attachments.

| Building control concept | Existing pattern | Decision |
|---|---|---|
| Inspection as a record with reference + status | `Defect` (DEF-#### reference, status enum, raised on the page or from a Control Centre email) | Mirror: `BuildingControlInspection`, project-qualified `BC-###` reference |
| Correspondence on the record | Mail tagging system — record tag stem, `RecordThreadTagger`, read live by category | New `RecordType` value(s) + `LinkableRecord` provider; no stored mail |
| Create record from an inspector's email | `CreateDefectFromMessage` / "Log Tender Enquiry" in the Control Centre | Same-shaped `CreateBuildingControlInspectionFromMessage` |
| Documents & photos on a record | `TenderEnquiryAttachment` (Upload \| Email source, private blob container, proxied download, `IsImage` thumbnails) + `TenderEnquiryEmailAttachmentFetcher` | Mirror: `BuildingControlAttachment` + attachment store; "copy attachments off this email" action |
| Case header (body, reference, key dates) | Project-scoped singleton records (e.g. Retention, Scheduling bucket) | New `BuildingControlCase` |
| Completion certificate at handover | `PracticalCompletion.CertificateBlobRef`, `HandoverPackItem.EvidenceBlobRef` (Closeout) | Surface the case's completion certificate to Close-Out later — no schema collision |
| Sidebar row / page shell / stores | `SidebarFolders` Project folder, `Refresh(projectId)` store convention, `LoadGate`/`Panel` loading rules, `Toolbar` conventions | One new row + page, standard conventions |

---

## 4. Proposed data model

Three new models in `contracts/Models/BuildingControl.cs` (+ entities, DbSets, one EF migration `AddBuildingControl`).

**`BuildingControlCase`** — the project's case with the building control body. One active case per project in the UI; the schema allows more than one (a second notice for an outbuilding, a re-submission) so we never have to migrate for it.
```
BuildingControlCaseId, ProjectId,
Regime: LocalAuthority | RegisteredApprover,
BodyName ("Bromley Building Control", "Assent BC"),
BodyReference ("BC2415406DOMFPB", "25-129527"),
ContactName, ContactEmail, ContactPhone,
Status: NoticeSubmitted | InForce | CompletionRequested | CompletionCertified | Lapsed,
NoticeSubmittedOn, AcceptedOn, CompletionCertifiedOn,   // user-entered official dates
Notes
```

**`BuildingControlInspection`** — one inspection stage. The register the tab is built around.
```
BuildingControlInspectionId, BuildingControlCaseId, ProjectId,
Reference,                 // "BC-001" — sequential per project; tag stem "JPMS/<projectRef>-BC-001"
StageName ("Foundations — ground beam reinforcement"),
Status: Planned | Booked | Inspected | Passed | ActionsRequired | Closed,
BookedFor,                 // date agreed with the inspector — user-editable, what lists lead with
InspectedAt,               // when the visit actually happened
OutcomeNotes,              // inspector's verbal outcome / actions required
InspectorName,
DisplayOrder, RaisedAt, RaisedByEmail
```
Status ladder: **Planned** (stage exists, no date) → **Booked** → **Inspected** (visit happened, outcome pending/verbal) → **Passed** or **Actions required** (fix and re-book — a re-inspection is the same record re-booked, or a new stage row if the body issues a fresh visit) → **Closed**. Per the two-dates convention: `BookedFor` is the official, user-editable date; `RaisedAt` is the system stamp, secondary on the detail page only.

**`BuildingControlAttachment`** — a file on the case or an inspection (exactly one parent set).
```
BuildingControlAttachmentId, ProjectId,
BuildingControlCaseId?, BuildingControlInspectionId?,
Kind: Photo | SiteInspectionReport | Notice | Acknowledgement | DecisionNotice |
      PlanningPermission | CompletionCertificate | Other,
FileName, ContentType, FileSizeBytes,
Source: Upload | Email,
AddedAt, AddedByEmail
```
Same private-container + proxied-download arrangement as tender-enquiry/bid-package attachments; `IsImage` drives photo thumbnails.

**Stage seeding.** New cases offer to seed a standard, editable stage checklist (Foundations → Drainage → Superstructure & roof → Insulation & fire lining → Completion) as `Planned` inspections; rows are freely renamed, reordered, added and deleted before anything is booked. A template, not a rule — the two example projects prove no fixed list survives contact with a real job.

---

## 5. Correspondence & Control Centre integration

Full mail integration from the start, on the standard model — the database never stores mail; records read it live by tag.

- **`RecordType` additions:** `BuildingControlInspection = 14` (and `BuildingControlCase = 15` for case-level threads — the "who's our contact" emails, notice acknowledgements — tag stem `JPMS/<projectRef>-BC`, mirroring how record-less tag families already work).
- **Tags are project-qualified** (`JPMS/JBB-2026-001-BC-001`), like request tags — inspection numbering restarts per project, and tags share one flat category space.
- **Control Centre:** the System Actions pane gains *link to an existing building control record* (via a `LinkableRecord` provider listing the case + its inspections) and *create an inspection from this email* (mirrors `CreateDefectFromMessage`: mints the record, tags the thread). Thread-tagging behaviour is unchanged — siblings tagged at triage time, later replies re-triaged with "Thread:" hints.
- **Attachments off the email:** on a linked email, "copy attachments to this inspection" pulls the inspector's PDF/photos into the attachment store with `Source = Email` (the `TenderEnquiryEmailAttachmentFetcher` pattern) — that's how site inspection reports mostly arrive.
- **Outbound:** booking-request emails are composed from the record via the existing draft-in-mailbox flow (nothing sent from code); the record's page shows the thread once triaged.

---

## 6. UI

One sidebar row — **Building Control**, in the Project folder after Defects — one page, one detail page.

**`/projects/{ProjectId}/building-control`** (`ProjectBuildingControl.razor`)
- **Case panel** at the top: body, regime, reference, contact, status chip, official dates, case documents (notice, acknowledgement, decision notice, completion certificate) with upload + download; an edit dialog. If no case exists yet, the panel is the "Set up building control" call to action — the page's one `btn-primary`.
- **Inspection register** below: table of stages — Reference, Stage, Status, Booked for, Inspected, Docs/photo count — ordered by `DisplayOrder`, led by `BookedFor` per the dates convention. Row click → detail. Toolbar of icon buttons (`ExportToExcelButton`, refresh) per the in-view toolbar convention; "Add inspection" is the register's create action.
- Standard conventions apply throughout: store `Refresh(projectId)` from `OnInitializedAsync`, nullable backing fields, one `LoadGate` per region with `UntilAll`, `dataFailed` opens the gate, no gated controls.

**`/projects/{ProjectId}/building-control/inspections/{InspectionId}`** (detail)
- Header: stage, status (one-click transitions along the ladder), booked/inspected dates, inspector, outcome notes.
- **Photos** grid (thumbnail via `IsImage`, upload from computer — on site this is the phone's browser) and **documents** list (site inspection report etc.).
- **Correspondence** panel reading the record's mail live by tag — same component family as requests/defects.
- `Created` (`RaisedAt`) shown only as a secondary fact here, never as a list's lead date.

**Close-Out tie-in (cheap, do it now):** when a case reaches `CompletionCertified` with a `CompletionCertificate` attachment, the Close-Out surface can link straight to it. No schema change — read-side only.

---

## 7. Build plan (after sign-off)

1. **Contracts** — `contracts/Models/BuildingControl.cs`; commands/queries under `contracts/BuildingControl/` (`CreateBuildingControlCase`, `UpdateBuildingControlCase`, `AddInspection`, `UpdateInspection`, `SetInspectionStatus`, `CreateBuildingControlInspectionFromMessage`, `ListBuildingControlForProject`); `RecordType` values.
2. **Persistence** — entities, DbSets, migration `AddBuildingControl` (**apply commands handed over with the code, scoped script from the last applied migration, per the prod migration rules**).
3. **API** — `api/Features/BuildingControl/` mirroring Closeout's shape: handlers/endpoints/validation/authorisation, `AzureBlobBuildingControlAttachmentStore`, identifier factory, reference minting.
4. **Mail** — `LinkableRecord` provider, tag stems, Control Centre link/create actions, attachment fetch from email.
5. **Frontend** — `IBuildingControlStore` + HTTP store + read model; the two pages; `SidebarRow` ("Building Control", `/projects/{project}/building-control`, `DirectorRoles` under the nav clamp).
6. **Verification** — unit tests on status transitions and reference/tag minting; recreate By France end-to-end as the acceptance script (case + 9 stages + photos + reports + completion) before showing it round.

Backfill of the existing SharePoint libraries is manual, through the upload UI, per project, as and when someone needs that project's history in the portal — no importer (consistent with the valuation spec's decision 4).

---

## 8. Decisions for the directors

1. **Stage seeding list** — agree the default checklist (proposal in §4), knowing every project can edit it freely.
2. **Re-inspection after "Actions required"** — re-book the same record (recommended: one stage, one row, full history in the thread) or mint a new row per visit?
3. **Who owns it once the nav clamp lifts** — proposal: PM and Site Manager raise/book/update; directors everything. API role sets built to that from day one even though nav is directors-only today.
4. **External visibility** — should the architect/client eventually see building control status (read-only) through the portal? Nothing in this build blocks it; worth knowing the intent.
5. **Chasing** — when an inspection sits at `Inspected` with no site report after N days, or `ActionsRequired` with nothing re-booked, should it raise a project To-do automatically? (Cheap now, noisy if wrong — happy to leave for v2.)
6. **Programme tie-in** — booked inspections shown on the Programme tab is a natural later phase; out of scope here unless the directors want it pulled forward.
