# Subcontractor CRM — Scope

**Status:** Built (pending compile verification + review) — Phase 0, hardening sweep, Features 1–3 implemented 2026-07-13
**Date:** 2026-07-13
**Depends on:** existing Subcontractors, Procurement and Variations features

## 1. Goal

Give subcontractors (P10) a self-service portal view of their own record, and give JBB a single place to manage the subcontractor relationship. Three capabilities:

1. Subcontractors upload documents to their own record.
2. Subcontractors see the work orders issued to them.
3. Subcontractors raise a variation against their work; on approval JBB issues a further work order (PO).

Decision taken: variations are **sub-requested, JBB-approved** — the subcontractor proposes and prices, the QS/PM approve through the existing VOQ → VO pipeline. Commercial control stays internal.

## 2. Current state

What already exists:

- `SubcontractorEntity` + `ComplianceDocumentEntity` (`api/Data/Entities/PeopleEntities.cs`) with `Kind`, `ExpiresAt` and computed Current/ExpiringSoon/Expired/Missing status (`contracts/Models/Subcontractor.cs`).
- Compliance upload endpoint `POST /subcontractors/{id}/compliance` (`api/Features/Subcontractors/Commands/UploadComplianceDocumentEndpoint.cs`) — **metadata only**, no file bytes stored. Blob storage pattern exists for drawings (`api/Features/Drawings/Storage/AzureBlobDrawingStore.cs`).
- `WorkOrderEntity` (`api/Data/Entities/ProcurementEntities.cs`) with `SubcontractorId`, lines with cost codes, statuses Draft → Released → Complete → Cancelled. Work orders **are** the purchase orders (WO-0001 numbering); no separate PO entity.
- Variations: `VariationOrderQuoteEntity` (Draft → Inviting → Tendering → Selected → Approved → Rejected) → `VariationOrderEntity` (Approved → Issued → Cancelled). Approval writes to the valuation report and CVR in one transaction (`ApproveVariationOrderQuoteHandler.cs`). Raising is restricted to internal roles (`api/Features/Variations/VariationRoles.cs`).
- RBAC: role-set gates per endpoint (`api/Gates/RoleSet.cs`), 12-role enum, session-cookie auth with invite links.

The gap:

- **No record-level scoping.** A `Role.Subcontractor` login is not tied to a `SubcontractorEntity`, so nothing prevents one sub seeing another's data. This is the foundational piece.
- No subcontractor-facing pages for work orders or variations; no way for a sub to initiate a variation.
- Compliance documents don't store the actual file.

## 3. Phase 0 — Record-scoped subcontractor access (foundation)

Decision taken: **link user → subcontractor** (persistent portal accounts, not per-invite tokens).

- Add nullable `SubcontractorId` to the user credential entity; migration + backfill n/a (no existing sub logins).
- New gate helper alongside `RoleSet`, e.g. `SubcontractorScope.Of(request)` that resolves the caller's `SubcontractorId` from the session and denies if absent. Every portal endpoint filters by it — never trust an id from the route alone.
- Invite flow: reuse the existing invite-token mechanism (`api/Auth/`, `AzureEmailInviteNotifier`) — Office & Compliance Coordinator (P07) invites a contact from the subcontractor record, which creates the credential pre-linked to that `SubcontractorId`.
- Front-end: portal pages live behind `Role.Subcontractor`; existing `DesktopNavigation` role filtering hides everything else. Mobile-first layout per the P10 persona (phone on site).

**Hardening sweep (done 2026-07-13).** Historically many endpoints authorised with only "is signed in"; external portal sessions made that untenable. Every endpoint under `api/Features` now carries a role gate. Shared sets live in `api/Gates/JpmsRoleSets.cs`: `AllInternal` (default for internal reads), `InternalAndArchitect` (RFI/variation reads — architect approves per the matrix), `DrawingReaders` (+ Architect + Subcontractor), `CommercialTeam` (cashflow). Narrower same-feature precedents were kept where they existed (`TriageRoles` on mailbox/triage, `AgentRoles` on agent queues, `RfiDashboardRoles`, `XeroLedgerRoles`). Only the five `/auth/*` endpoints remain role-free by design. Follow-ups flagged during the sweep: (a) `ListProjectsVisibleToUser` does not actually self-scope per user despite the name — full project list to any internal role; (b) subcontractors can raise requests but can't read them back (`InternalAndArchitect` on request reads) — the portal will need its own scoped RFI read; (c) the portal-invite endpoint permits Director/PM/OCC but the invite button lives on Admin/MD-only `SubcontractorDetail.razor` — widen when OCC take over onboarding.

## 4. Feature 1 — Document uploads to own record

- Extend `ComplianceDocumentEntity` with `BlobPath`; new `AzureBlobComplianceDocumentStore` following the drawings store pattern. Same container conventions, path `subcontractors/{subcontractorId}/{documentId}/{filename}`.
- **Versioning:** re-uploading a document of the same `Kind` creates a new version rather than replacing. Add `Version` (int) and `SupersededAt` to `ComplianceDocumentEntity`; latest version drives expiry status, prior versions remain downloadable for audit.
- New portal endpoints (subcontractor-scoped):
  - `GET /portal/my/documents` — list own compliance documents with expiry status.
  - `POST /portal/my/documents` — upload file + `Kind` + `ExpiresAt`.
  - `GET /portal/my/documents/{id}/content` — download own file.
- Internal side unchanged: P07 continues to see/manage documents via `SubcontractorDetail.razor` / `SubcontractorComplianceList.razor`; add download there too.
- Nudge value: expiring documents surface on the portal home so subs stay current "without being chased" (persona pain point).
- New portal page `Pages/Portal/MyDocuments.razor`; new `IPortalStore` following the store convention — `Refresh()` called once from `OnInitializedAsync`, cached reads for render (per CLAUDE.md).

## 5. Feature 2 — Issued work orders view

- New query `ListWorkOrdersForSubcontractor(subcontractorId)` in `api/Features/Procurement/Queries/`, filtered to `Status >= Released` (subs never see Drafts) and to the caller's `SubcontractorId`.
- Returns: number (WO-0001), project name, title, scope, value, line items with `LineTotal`/`PaidToDate`, scheduled completion, linked variation reference if the WO originated from a VO.
- Portal page `Pages/Portal/MyWorkOrders.razor` (list) + detail view. Read-only in this scope.
- Optional (recommended, small): email the subcontractor contact when a WO moves to Released — infra exists (`AzureEmailInviteNotifier` pattern); today nothing notifies subs of issuance.

## 6. Feature 3 — Variation request → further purchase order

New entity, feeding the existing pipeline rather than bypassing it:

**`SubcontractorVariationRequestEntity`** (new, `api/Data/Entities/VariationEntities.cs`):

- `SubcontractorId`, `ProjectId`, `WorkOrderId` (the WO being varied), `Title`, `Description`, `ProposedValue`, proposed line items, photo/document attachments (blob), `Status`, timestamps, `ReviewedBy`, `RejectionReason`, `VariationOrderQuoteId` (set on acceptance).
- Statuses: `Submitted → UnderReview → Accepted | Rejected | Withdrawn`.

Flow:

1. **Sub raises** from a work order in the portal: `POST /portal/my/work-orders/{id}/variation-requests` with description, priced lines, attachments.
2. **QS/PM review** on a new "Variation requests" section of `ProjectVariations.razor` (internal). Reject with reason (visible to sub) or **accept**, which creates a VOQ pre-populated from the request — sub's price captured as the bid, so the normal Selected → Approved path applies. `VariationRoles` unchanged; the VOQ is still raised by an internal role, triggered by acceptance.
3. **Approval** runs the existing `ApproveVariationOrderQuoteHandler` (VO created, valuation + CVR updated — no changes needed there).
4. **Further PO:** new command `IssueWorkOrderForVariationOrder` — creates a `WorkOrderEntity` (status Released) for the same subcontractor, referencing the VO (add `VariationOrderId` FK to `WorkOrderEntity`), next sequential WO number. Always a **new** work order — existing WOs are never uplifted; a variation may require one or more new WOs to complete the work. The sub sees it appear under Feature 2, closing the loop.
5. Status of the request is mirrored to the portal (`GET /portal/my/variation-requests`) so subs can track Submitted → Approved → PO issued.

Terminology note: keep "variation order quote" / "variation order" / "work order" as the code terms; the "further purchase order" the business asks for **is** the new work order.

## 7. RBAC / permissions changes

| Action | Roles |
|---|---|
| Portal endpoints (`/portal/my/*`) | Subcontractor only, + `SubcontractorScope` record filter |
| Invite subcontractor user | OfficeComplianceCoordinator, ProjectManager, Director |
| Review/accept variation requests | Per `VariationRoles` (Admin, Director, ProjectManager, Estimator) |
| Issue WO for approved VO | Same as existing WO creation roles |

Update `docs/05-data-model/permissions-matrix.md` P10 column when built.

## 8. Build order

1. **Phase 0** — user ↔ subcontractor link, `SubcontractorScope` gate, invite flow, empty portal shell + navigation. *Everything else depends on this.* ✅ Built.
2. **Feature 1** — documents (blob storage plumbing reused by Feature 3 attachments). ✅ Built (attachments on variation requests deferred).
3. **Feature 2** — work orders view (read-only, small). ✅ Built.
4. **Feature 3** — variation request entity, portal submission, internal review UI, accept→VOQ bridge, `IssueWorkOrderForVariationOrder`. ✅ Built. Acceptance creates the VOQ directly in **Selected** state (the sub's price is the tender), so the unchanged `ApproveVariationOrderQuote` pipeline does the valuation/CVR writes; issuing the WO also adds a single cost-coded `WorkOrderLine` so allocation views see the value, and moves an Approved VO to Issued.
5. Notifications (WO released, variation request status changes) — cut-line candidate, **not built**.

Implementation notes: `UnderReview` status exists in the enum but nothing transitions to it yet; variation-request file/photo attachments deferred (blob plumbing exists); the endpoint hardening sweep (see §3) landed alongside Phase 0.

## 9. Out of scope

- Payments to subcontractors (stays in Xero/AP per persona notes) and valuation invoices.
- Timesheets, RFIs, RAMS/induction acceptance in the portal (already-planned P10 features, separate work).
- Bid/tender submission through the portal (existing bid-package invite flow continues).
- Offline support for the portal (persona flags it; defer).

## 10. Decisions (resolved 2026-07-13)

- **One login per subcontractor.** Users already support multiple roles, so no need for multiple portal logins per company. The `SubcontractorId` link stays 1:1 with the invited contact; the model doesn't preclude adding a second login later if ever needed.
- **Documents are versioned on re-upload**, never replaced (see §4).
- **Variations always issue new work orders.** A variation to the contract can require one or more new WOs to complete the work; existing WOs are never uplifted (see §6).
