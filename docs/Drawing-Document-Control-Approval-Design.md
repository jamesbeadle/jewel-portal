# Drawing Document Control — Approval Workflow & Blob Upload (Design)

**Status:** Implemented · **Scope:** Workflow 01 (Drawing & Document Control) · **Date:** 2026-07-01

> Implemented per §7 build order. One design refinement during build: `RegisterDrawing` no longer
> carries `InitialRevisionLabel` (a drawing has no approved revision at registration — the label is
> set on first approval), so that field was dropped from the command, validation and callers.

This document describes how to add an **approval-driven versioning workflow** and **real file upload (Azure Blob Storage)** to the existing Drawings feature, and the project views that fall out of it: list drawings, filter to approved, see the latest approved version of a thing, and drill into its archived history.

It is deliberately scoped to **drawings only**. The design keeps the seams clean so a generic "document type" layer (RAMS, certificates, submittals, …) can be lifted out later without reworking the approval engine — but no generic abstraction is built now.

---

## 1. Where the code is today

The feature already exists and is closer to the target than a greenfield build. Current shape:

| Concept | Current implementation |
|---|---|
| **Drawing** (the "thing" — Garden, Shed) | `DrawingEntity` per project: `DrawingCode`, `Title`, `CurrentRevision`, `CreatedAt`. |
| **Version** | `DrawingRevisionEntity`: `RevisionLabel`, `FileName`, `IssuedByEmail`, `ReceivedAt`, `SupersededAt?`, `IsAmbiguous`, `ViewCount`. |
| **Commands** | `RegisterDrawing`, `UpdateDrawingMetadata`, `IssueDrawingRevision`. |
| **Queries** | `ListDrawingsForProject`, `GetDrawingById`, `ListRevisionsForDrawing`. |
| **Frontend** | Blazor WASM `IDrawingStore` / `HttpDrawingStore` / `DrawingsReadModel`, route registration. |

**Two gaps against the requirement:**

1. **No approval status.** `IssueDrawingRevisionHandler` supersedes *every prior active revision the instant a new one is uploaded* (`SupersededAt = now`). The requirement is the reverse: a new version is uploaded **Unapproved**, the existing approved version stays "latest", and archiving only happens **on approval**.
2. **No file storage.** Only a `FileName` string is persisted. `BlobUri` / `*BlobRef` columns exist on *other* entities, but there is **no `Azure.Storage.Blobs` code anywhere in the repo** and `AzureWebJobsStorage` is set to `UseDevelopmentStorage=true`. Real upload is genuinely new work.

### Architecture the design must follow

Vertical-slice CQRS on Azure Functions (.NET 8 isolated), EF Core against Azure SQL with **migrate-on-startup** (`context.Database.MigrateAsync()` in `Program.cs`), Blazor WASM on Azure Static Web Apps, plus a separate Functions **worker** for out-of-band jobs. Each command slice is four files: contract record (`contracts/Drawings/*.cs` implementing `ICommand<T>`), `*Handler`, `*Validation`, `*Authorisation`, wired through a thin `*Endpoint` (`HttpTrigger`) and registered in `DrawingsFeatureRegistration`. Identifiers are compact GUIDs from `DrawingIdentifierFactory`. New slices must match this exactly.

---

## 2. Target model

### 2.1 Status set

A single explicit status on each revision — **`Unapproved → Approved → Archived`** (the set you chose):

```
Unapproved  ── approve ──►  Approved  ── (a newer revision approved) ──►  Archived
```

- **Unapproved** — uploaded, pending review. Multiple Unapproved revisions of the same drawing can coexist.
- **Approved** — the current, canonical version of that drawing. **At most one Approved revision per drawing at any time** (enforced by the approval handler, below).
- **Archived** — was approved, has since been replaced; read-only history.

No `Rejected` / `Superseded` states in this pass (you opted for the minimal set). "Superseded" is folded into **Archived**; a reject is simply a revision that never gets approved. If explicit rejection is wanted later it slots in as a fourth enum value without disturbing anything.

### 2.2 The one behavioural rule that changes

> **When a revision is approved, it becomes the latest approved version and every other revision of that drawing is Archived.**

This matches the requirement's wording ("when a new drawing is approved it becomes the latest drawing with all other drawings that were for that thing becoming archived"). Concretely, approving revision *R* of drawing *D*:

1. Set *R* → `Approved`, stamp `ApprovedByEmail` + `ApprovedAt`.
2. Set **every other** revision of *D* whose status is not already `Archived` → `Archived`, stamp `SupersededAt = now`. This sweeps up both the previously-approved version **and** any lingering Unapproved siblings (confirmed), so exactly one live version remains.
3. Set `Drawing.CurrentApprovedRevisionLabel = R.RevisionLabel`.

Uploading a revision no longer touches sibling revisions — that responsibility moves entirely to approval.

---

## 3. Schema changes

One migration: **`AddDrawingApprovalAndBlob`**.

**`DrawingRevisionEntity`** — add:

| Column | Type | Notes |
|---|---|---|
| `ApprovalStatus` | `int` (enum) | `0 = Unapproved`, `1 = Approved`, `2 = Archived`. Default `0`. |
| `BlobRef` | `nvarchar(1024)` null | Storage path of the uploaded file (see §5). Null until a file is attached. |
| `ContentType` | `nvarchar(128)` null | e.g. `application/pdf`, for correct download headers. |
| `FileSizeBytes` | `bigint` null | For display / integrity. |
| `ApprovedByEmail` | `nvarchar(256)` null | Who approved. |
| `ApprovedAt` | `datetimeoffset` null | When approved. |

`SupersededAt` is retained and now means "archived at". `IsAmbiguous` / `ViewCount` unchanged.

**`DrawingEntity`** — the existing `CurrentRevision` column is **repurposed** to hold the latest *approved* revision label (renamed `CurrentApprovedRevisionLabel`, made nullable — null when a drawing exists but nothing is approved yet). One field, one meaning; no redundant column is kept. The rename is a non-destructive EF column rename in the migration, so existing values carry over.

**Backfill.** Existing rows have `ApprovalStatus = 0 (Unapproved)` by default. A one-off data step in the migration sets each drawing's currently-non-superseded revision to `Approved`, so current data keeps a sensible "latest approved" (the repurposed label column already holds the right value). Because migrations run on startup, this backfill ships as raw SQL in the migration's `Up`.

Enum lives in `contracts` alongside the models: `public enum DrawingApprovalStatus { Unapproved = 0, Approved = 1, Archived = 2 }`. `DrawingRevision` and `Drawing` records gain the corresponding fields; `DrawingEntityMapping.ToModel` is extended.

---

## 4. Commands & queries

### 4.1 New / changed commands

| Slice | Route | Behaviour | Roles |
|---|---|---|---|
| **`UploadDrawingRevision`** *(replaces `IssueDrawingRevision`)* | `POST /api/drawings/{drawingId}/revisions` (multipart/form-data) | Streams the file to blob storage, creates the revision as **`Unapproved`** with `BlobRef`/`ContentType`/`FileSizeBytes`. **Does not** supersede siblings. | Admin, Managing Director, Project Manager |
| **`ApproveDrawingRevision`** *(new)* | `POST /api/drawings/{drawingId}/revisions/{revisionId}/approve` | Applies the §2.2 rule: target → Approved, all others → Archived, updates the current-approved label. | Admin, Managing Director, Project Manager |
| `RegisterDrawing` | unchanged | Creates the named container. | unchanged |
| `UpdateDrawingMetadata` | unchanged | Edit code/title. | unchanged |

`IssueDrawingRevision` is renamed to `UploadDrawingRevision` to reflect that it no longer "issues" (supersedes). Its contract changes from a JSON body to multipart (file + fields). Keep the audit intent of the existing `DrawingIssueRecord` model — record who uploaded and, separately, who approved (the `ApprovedBy*` columns cover the approval half).

Each new slice follows the four-file convention (`Handler` / `Validation` / `Authorisation` / `Endpoint`) and is added to `DrawingsFeatureRegistration`. Both slices gate on the same role set — `RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager)` (Administrator, Managing Director, Project Manager). Administrators already receive every role server-side, but `Role.Admin` is listed explicitly for clarity, matching the `TriageRoles` precedent.

### 4.2 Queries (the views you asked for)

| Query | Route | Returns |
|---|---|---|
| `ListDrawingsForProject` *(extended)* | `GET /api/projects/{projectId}/drawings?approvedOnly={bool}` | One row per drawing with its **latest approved** revision (label, file, approvedAt) and counts (`unapprovedCount`, `archivedCount`). `approvedOnly=true` returns only drawings that have an approved revision. This is the **project list + "filter by approved"** view. |
| `ListRevisionsForDrawing` *(extended)* | `GET /api/drawings/{drawingId}/revisions?status={all\|approved\|unapproved\|archived}` | Revisions with their `ApprovalStatus`. `status=archived` powers the **drill-in "archived history"** view. |
| `GetDrawingById` | unchanged | The container + current approved label. |

No new query *slices* are strictly required — the two extensions above cover list, filter-by-approved, latest-approved, and archived drill-in. A dedicated `GetLatestApprovedRevision(drawingId)` can be added if the frontend wants it standalone, but the list projection already carries it.

---

## 5. File storage on Azure Blob

Nothing blob-related is wired today, so this is additive and self-contained.

**Provisioning (infra).** Add to `azure-setup.sh` / `azure-prod-setup-v2.sh`:

- A **Storage Account** (`Standard_LRS` test, `Standard_GRS` prod) in the resource group.
- A **private** blob container `drawings`.
- Write the connection string into `.azure-output.env` and set it as the SWA/Functions app setting **`DrawingsStorage:ConnectionString`** (mirroring how `SqlConnectionString` is handled). Locally it points at Azurite (`UseDevelopmentStorage=true`), which is already the `AzureWebJobsStorage` value.

**Abstraction.** A small `IDrawingBlobStore` in the api with an `AzureBlobDrawingStore` implementation over `Azure.Storage.Blobs` (add the NuGet package to `JpmsApi.csproj`). Interface:

```csharp
Task<string> UploadAsync(string projectId, string drawingId, string revisionId,
                         string fileName, string contentType, Stream content, CancellationToken ct);   // returns BlobRef
Task<(Stream Content, string ContentType, string FileName)> OpenAsync(string blobRef, CancellationToken ct);
```

**Blob layout:** `{projectId}/{drawingId}/{revisionId}/{originalFileName}` in the `drawings` container — human-navigable and collision-free (revisionId is a GUID).

**Upload path.** `UploadDrawingRevisionEndpoint` reads `multipart/form-data` (`request.Form.Files[0]` + form fields), calls `UploadAsync`, then the handler persists the revision with the returned `BlobRef`. Blob write happens **before** the DB row is committed so a failed upload never leaves a dangling Unapproved revision.

**File size — no application-imposed cap** (confirmed): the platform limits apply. A single Azure block blob goes far beyond any drawing (hundreds of GiB), so blob storage is never the constraint. The real ceiling is the **Azure Functions HTTP request body limit** (~100 MB for a buffered request on the managed/SWA-linked plan). Drawings almost never approach this, so the streamed multipart upload above is the v1 path with no cap enforced in code. If a genuinely huge file ever needs to go in, the fallback is **direct-to-blob via a short-lived SAS URL** (browser uploads straight to the container, the API only records the `BlobRef`) — noted as the escape hatch, not built now.

**Download path.** New query-side endpoint `GET /api/drawings/revisions/{revisionId}/file`:
- Auth-checked (project membership), looks up `BlobRef`, and **proxies** the stream back with the stored `ContentType` and a `Content-Disposition` filename. Proxying (rather than handing out a public URL) keeps the container private and lets us increment `ViewCount` / capture the on-site acknowledgment that Workflow 01 already anticipates (`AcknowledgeDrawingViewedOnSite`). A short-lived **SAS URL** is the alternative if we later need the browser to stream large PDFs directly — noted, not built now.

---

## 6. Frontend (Blazor WASM)

Within the existing `jpms/Features/Drawings` slice and `HttpDrawingStore`:

- **Drawing register (project view).** Table of drawings; each row shows title/code, the **latest approved** revision label + date, and status chips (`Approved` green, `n pending` amber, `n archived` grey). A **"Approved only"** toggle drives `approvedOnly=true`.
- **Upload.** Drag-and-drop / file picker posting `multipart/form-data` to `UploadDrawingRevision` with title/code (new drawing) or against an existing drawing, plus revision label. New uploads land as **Unapproved** and surface in a "Pending approval" group.
- **Approve.** On an Unapproved revision, an **Approve** action (role-gated, hidden otherwise) calls `ApproveDrawingRevision`; on success the row moves to Approved and siblings drop into Archived.
- **Drill-in / history.** Selecting a drawing shows the current approved version prominently with a **Download**, and an **Archived** section (`status=archived`) listing superseded versions, each downloadable and read-only.

`DrawingsReadModel` gains `approvedOnly` state and a status filter for the revisions list; `IDrawingStore` gains `UploadRevisionAsync(file, …)` and `ApproveRevisionAsync(drawingId, revisionId)`.

---

## 7. Build order

1. **Contracts + enum + model fields** (`contracts/Drawings`, `contracts/Models/Drawing.cs`).
2. **Entity columns + migration** `AddDrawingApprovalAndBlob` (+ backfill SQL). Verify it applies clean on startup against a scratch SQL DB.
3. **Blob store** (`IDrawingBlobStore` + Azure impl + NuGet + DI + local Azurite settings).
4. **`UploadDrawingRevision`** slice (replaces `IssueDrawingRevision`; multipart endpoint).
5. **`ApproveDrawingRevision`** slice (the §2.2 rule).
6. **Query extensions** (`approvedOnly`, `status` filter, latest-approved projection) + **download endpoint**.
7. **Infra**: storage account + container + app setting in the setup scripts.
8. **Frontend**: register table + filter, upload, approve, drill-in/history, download.
9. **Verification** (below).

## 8. Decisions (confirmed)

1. **Who uploads and changes drawing status:** **Administrator, Managing Director, Project Manager** (`Role.Admin`, `JpmsRoles.Director`, `JpmsRoles.ProjectManager`). Both upload and approval gate on this set.
2. **Stale Unapproved siblings on approval:** **archived** — approving a revision archives every other revision of that drawing, leaving exactly one live version.
3. **File size:** **no application cap** — platform limits only (see §5).
4. **Data structure:** `Drawing.CurrentRevision` is repurposed to the single "current approved revision label" field (§3) — no redundant columns; the structure carries exactly what the workflow needs.

## 9. Verification plan

- **Migration**: apply against a scratch Azure SQL / LocalDB, confirm backfill sets exactly one Approved revision per existing drawing and populates `CurrentApprovedRevisionLabel`.
- **Approval invariant** (integration test): after any `ApproveDrawingRevision`, assert **exactly one** `Approved` revision exists for the drawing and all others are `Archived`.
- **Upload isolation**: uploading a revision leaves sibling statuses unchanged and creates the blob before the DB row.
- **Views**: `approvedOnly=true` excludes drawings with no approved revision; `status=archived` returns only superseded versions; download streams the right bytes with the right content type.
- **Auth**: non-authorised roles get `403` on upload/approve; download enforces project membership.
- **Blob failure**: a forced upload failure leaves no orphaned revision row.
