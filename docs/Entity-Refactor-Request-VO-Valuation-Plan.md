# Entity Refactor — Request · Variation Order · Valuation Report

**Status:** Draft for review
**Author:** Cowork (for Nigel Reilly)
**Date:** 1 July 2026
**Source artefact:** hand-drawn entity/flow diagram (1 July 2026), transcribed in §1.

---

## 0. Why this document

Today a "request" is a single overloaded record (`RequestEntity` with a `RequestType` enum) that stretches from an untyped tagged email all the way to something that implies a variation. The diagram asks us to make the **three real business abstractions** explicit and to wire the flow that connects them:

1. **Request** — the triage-level record an email becomes.
2. **Variation Order** — the priced, approved change to the contract.
3. **Valuations Report (CVR)** — the commercial roll-up and the monthly cash call.

This is a **plan only** — no code in this pass. It states the target model, the new entities, the state machines, the CQRS surface, the migrations, and the wiring, all expressed in the existing codebase's names so it can be picked up directly. It reconciles with the three in-flight specs (`Valuation-Report-Tab-Spec.md`, `Bid-Package-Invite-and-Agent-System-Spec.md`, `Linkable-Record-Container-Plan.md`).

---

## 1. The diagram, transcribed

```
RFI ───────────── "Can Be" ─────────────┐
                                         │
[RFQ] ─────────── "Can have" ────────────┤►  REQUEST
                                         │
Variation Order Quote (VOQ) ─ "Can Create"┘
        │
        └─ "Can trigger" ─ ─ ─ ─ ─ ─ ─ ─►  VARIATION ORDER
  [Bid Package Invite]                            ▲
        │                          "Approve Variation Order"
  [Bid Package Tendering]                         │
        │                                         │
  Selected Bid Package Tender ───────────────────┘
                                                  │
                                          "Updates / Add VO to CVR"
                                                  ▼
                                        VALUATIONS REPORT (CVR)
                                                  │
                                        "Create Cashcall (monthly)"
```

Read as sentences:

- A **Request** *can be* an **RFI**.
- A Request (RFI) *can have* an **RFQ**.
- An **RFQ** *can create* a **Variation Order Quote (VOQ)**.
- A **VOQ** holds an array of **Bid Packages** → *Bid Package Invite* → *Bid Package Tendering* → *Selected Bid Package Tender*, and *can trigger* / on **Approve Variation Order** produce a **Variation Order**.
- A **Variation Order** *updates* the **Valuations Report (CVR)** — *"Add VO to CVR"*.
- The **Valuations Report** *creates a monthly Cash Call*.

---

## 2. Target entity model (overview)

```
Client (global)                         Project
  └─ architectEmail  ────────┐            │
                             ▼            ▼
                         Request  ── (RecordType.Request)
                             │  type: General → RFI → RFQ
                             │  optional Documents (blob) + audit trail
                             ▼  (RFQ creates)
                   VariationOrderQuote (VOQ)
                             │  1..* BidPackage  (existing entity, now VOQ-scoped)
                             │        └─ Invite → Tendering → Selected tender
                             │  selected sub + budget → CostCodeBudget
                             ▼  (approve)
                      VariationOrder (VO)
                             │  writes ValuationLineItem rows (ElementType.Variation)
                             │  feeds QsAccrual / CvrSnapshot
                             ▼
                 Valuations Report / CVR  (existing valuation-report + CVR)
                             │
                             ▼  (monthly)
                        CashCall  ──► project-level running total
```

Guiding principle (same as the Valuation-Report tab spec): **assemble existing patterns**, add first-class records only where the business genuinely has one. New first-class records: **VariationOrderQuote**, **VariationOrder**, **CashCall**, **Client**. Everything else is an extension of what exists.

---

## 3. Current state (what exists, with file paths)

| Concept | Current implementation | Verdict |
|---|---|---|
| Request | `RequestEntity` + `Request` record; `RequestType {Rfi,Rfa,Rfc,NoticeOfDelay,Rfq,Rfp,ExtensionOfTime}`; `RequestStatus`; `ImpliesVariation`; `REQ-NNNN` numbering; mailbox tag `JPMS/<TagReference>` (`contracts/Models/Request.cs`, `api/Data/Entities/ProcurementEntities.cs`) | Keep; extend with `General` default + promotion flow |
| Request document | `RequestDocumentBuilder`/`RequestDocumentRenderer`, auto-send on raise via `SendRequestDocument` mailbox action (`api/Features/Requests/Documents`, `docs/request-documents.md`) | Reuse for RFI-to-architect email |
| Email triage → record | Graph tagging, live read-back, `CreateRequestFromMessage`, record-agnostic `ILinkableRecordProvider` / `RecordType` (`api/Features/RecordLinks`, `api/Features/MailboxIntake`, `contracts/Models/LinkableRecord.cs`) | Reuse; add VOQ/VO providers |
| Architect contact | `ProjectContact` with `Role=Architect`, `ReceivesRequests` flag — **per project**, no global client (`contracts/Models/ProjectContact.cs`) | Gap: needs a global Client holding architect email |
| Bid packages | `BidPackage`/`BidPackageLineItem`/`BidPackageRecipient`/`Quote`/`WorkOrder` (`contracts/Models/Procurement.cs`, `api/Data/Entities/ProcurementEntities.cs`) | Keep; add optional `VariationOrderQuoteId` link |
| Directory | `Subcontractor` unified by `DirectoryCategory {Subcontractor,Client,Architect,Supplier,Other}` (`contracts/Models/Subcontractor.cs`) | Reuse for sub selection + architect/client sourcing |
| Variation Order | **None as a first-class record** — only `ValuationLineItem.ElementType=Variation` + `VariationRef` (`api/Data/Entities/ValuationReportEntities.cs`) | **Build new** |
| Cost centre / budget | Global `CostCenterEntity` (code/name); per-project `CostCodeBudgetEntity {CostCode,AllocatedAmount,SpentAmount}` (`api/Data/Entities/CommercialEntities.cs`, `CostCenterEntity.cs`) | Reuse; add committed amount + VOQ→budget write |
| Valuation report | `ValuationLineItem`/`ValuationClaim`/`ClaimLine` (`Valuation-Report-Tab-Spec.md`, `api/Data/Entities/ValuationReportEntities.cs`) | Reuse as the CVR/report target |
| CVR | `CvrSnapshotEntity` + `QsAccrual`/`ForecastComponent`/`Eot`/`Prelim` inputs (`contracts/Cvr/*`) | Reuse; VO approval feeds it |
| Cashflow | `CashflowSnapshotEntity` (13-week) (`api/Data/Entities/CashflowEntities.cs`) | Keep; distinct from Cash Call |
| Cash Call | **None** — cash "called up" is only `ValuationClaim.CertifiedToDate` | **Build new** (project-level running total) |
| Document upload / blob | **None** — PDFs regenerated in-memory; no blob container (`docs/request-documents.md`, Bid-Package spec Documents section = deferred) | **Build new** (Azure Blob) |

**Platform primitives available** (from infra review): Azure SQL + EF Core 8 with auto-migrate on startup (`api/Program.cs`); CQRS = contract record → `ICommandHandler`/`IQueryHandler` → `*Authorisation` + `*Validation` → Azure Function endpoint, self-registered via `Add<Feature>Feature()` in `Program.cs`; email via Azure Communication Services (`api/Auth/AzureEmailInviteNotifier.cs`) and Microsoft Graph `Mail.Send` for mailbox documents; async via Azure Storage Queues (`api/Features/MailboxIntake/Queue`); PDF via PDFsharp/MigraDoc. **No blob storage and no domain-event bus exist yet** — see §9.

---

## 4. Entity-by-entity design

### 4.1 Request — add the "General" default and the promotion ladder

The description makes **General the default state** of every request (project-tagged, cost-centre known, everything else optional), with promotion to RFI, then the ability to spawn an RFQ.

Changes:

- Add `RequestType.General = 7` (existing integer values stay pinned — see the comment in `Request.cs`). New requests created from triage default to `General`.
- Promotion is a command, not a free edit, because promotion has side effects:
  - `PromoteRequestToRfi(requestId)` → sets `Kind=Rfi`, and **sends the official RFI email to the architect**. The architect address resolves from the **Client account** (§4.5) with fallback to the project's `ProjectContact{Role=Architect, ReceivesRequests=true}`. Reuses the existing `SendRequestDocument` mailbox action + `RequestDocumentRenderer`.
  - `EnableRfqOnRequest(requestId)` (only valid when `Kind=Rfi`) → marks the request as carrying an RFQ (`HasRfq=true`) and unlocks VOQ creation.
- Keep `ImpliesVariation` as the analyst signal that this request is heading for a variation.

New fields on `RequestEntity` / `Request`:

```
RequestType Kind          // + General = 7
bool HasRfq               // RFI has spawned an RFQ (gates VOQ creation)
string? ClientId          // owning client account (architect email source)
```

State ladder:

```
General ──promote──► RFI ──enable RFQ──► RFI+RFQ ──create VOQ──► (VOQ owns the rest)
   default             sends architect email        unlocks §4.2
```

### 4.2 Variation Order Quote (VOQ) — new record

Created from an RFQ ("an RFQ automatically creates a VOQ"). A VOQ is the **procurement container**: an array of bid packages sent to subcontractors, the place where the winning sub and the budget are captured, and the thing that, when approved, produces a Variation Order.

New model `VariationOrderQuote` (`contracts/Models/Commercial.cs` or a new `contracts/Models/Variations.cs`):

```
VariationOrderQuoteId, ProjectId,
RequestId,                 // the RFQ it was created from
Reference,                 // e.g. VOQ-0001 (drives mailbox tag + RecordType)
Title, Description,
Status: Draft | Inviting | Tendering | Selected | Approved | Rejected,
SelectedBidPackageId,      // the winning package/tender (nullable until Selected)
SelectedSubcontractorId,   // stored winning sub (nullable until Selected)
EstimatedValue,            // rolled up from selected tender
CreatedAt, CreatedByEmail,
ApprovedAt, ApprovedByEmail
```

Bid packages become VOQ-scoped: add `string? VariationOrderQuoteId` to `BidPackageEntity`/`BidPackage` (nullable → keeps existing standalone packages valid). The existing `BidPackage → BidPackageRecipient → Quote → WorkOrder` chain and the `BidPackageStatus {Draft,Inviting,QuotesReceived,Comparing,Awarded}` lifecycle drive "Invite → Tendering → Selected tender" with no new procurement machinery.

**RFQ line items match budgeted line-item categories.** The VOQ's bid-package `BidPackageLineItem`s carry a `CostCode` that must resolve to an active `CostCenterEntity`, so the quote lines line up with the budget categories they will later spend against (§4.6).

### 4.3 Bid Package Invite within a VOQ

Reuse the Bid Package Invite work already specced in `Bid-Package-Invite-and-Agent-System-Spec.md` (Part B): recipients from the directory modal (`DirectoryCategory.Subcontractor`/`Supplier`), the invite question set (deposit/duration/availability/insurances/RAMS/portfolio), tagged-email intake, and the future AI draft. The only delta here is the **parent link** (`VariationOrderQuoteId`) so multiple packages roll up under one VOQ. `RecordType.BidPackageInvite` and its agent stay as specced.

### 4.4 Variation Order (VO) — new record

Created on **Approve Variation Order** from a VOQ. This is the first-class record that today is only a row on the valuation bill.

New model `VariationOrder`:

```
VariationOrderId, ProjectId,
VariationOrderQuoteId,     // the VOQ it was approved from
RequestId,                 // provenance back to the originating request
VariationRef,              // "V18" — reused as ValuationLineItem.VariationRef
Title, Description,
Status: Approved | Issued | Cancelled,
Value,                     // net value of the variation
SubcontractorId,           // who is doing the work (from the selected tender)
CostCode,                  // budget category
ApprovedAt, ApprovedByEmail
```

On approval (`ApproveVariationOrder`), the handler, in one transaction:

1. Creates the `VariationOrder`.
2. Writes the corresponding **`ValuationLineItem` rows** (`ElementType=Variation`, `VariationRef=VO.VariationRef`, `VariationTitle`, `CostCode`, priced lines from the selected tender) — this is the diagram's **"Add VO to CVR"**.
3. Records a **`QsAccrual`** / updates `CostCodeBudget` committed spend so the **CVR** reflects the new committed cost (§4.6), and optionally captures a fresh `CvrSnapshot`.
4. Advances the VOQ to `Approved`.

### 4.5 Client account + architect email — new global entity

Promotion to RFI must email "the architect email stored within the **client account**." Today that email only lives per-project on `ProjectContact`. Add a global **Client**:

```
Client: ClientId, Name, PrimaryContactName, PrimaryContactEmail,
        ArchitectName, ArchitectEmail, CreatedAt
Project: + ClientId   // link project to its client account
```

Resolution order for the RFI recipient: `Client.ArchitectEmail` → project `ProjectContact{Role=Architect, ReceivesRequests}` → error surfaced in validation. (The directory already has `DirectoryCategory.Client`/`Architect`; the address-book modal in the Bid-Package spec wants clients/architects too — a `Client` entity satisfies both. Decision D-A in §11 covers Client-as-directory-record vs standalone.)

### 4.6 Cost centres, budgets and line-item matching

The wiring the diagram implies ("stored … the budget into the cost centres and the line items of the RFQ match the budgeted line item categories"):

- Every `BidPackageLineItem.CostCode` (on a VOQ) and every `VariationOrder.CostCode` must reference an **active `CostCenterEntity.Code`** (validation, not yet an FK — mirrors the current soft-link convention).
- On VOQ **selection**, write/refresh the project `CostCodeBudgetEntity` for the selected tender: allocate the tender value into the matching cost code. Add a **`CommittedAmount`** column to `CostCodeBudgetEntity` (this is open decision **D5** in the Valuation-Report spec — resolve it here) so committed-but-not-spent VO value is visible separately from `SpentAmount`.
- The valuation report's variation lines already group by `CostCode`, so the CVR, the budget and the VO all reconcile on the same code.

### 4.7 Cash Call — new, monthly, project-level running total

"A cash call procedure can be done every month; the amount available reduces as the client invoice is prepared; the system stores total amounts when cash calls are received to increase the cash call total at the project level."

This is distinct from the 13-week `CashflowSnapshot` and overlaps with `ValuationClaim` (the "cash called up" record). Recommended model (see decision D-C):

```
CashCall: CashCallId, ProjectId,
          PeriodMonth,               // the month this call covers
          ValuationClaimId,          // the claim/invoice it draws from (nullable)
          AmountRequested,           // what we call up (reduces "available")
          AmountReceived,            // what the client actually paid
          Status: Requested | Invoiced | Received,
          RequestedAt, ReceivedAt

Project:  + CashCallTotal            // running Σ AmountReceived across confirmed cash calls
```

Flow: monthly `CreateCashCall` (drawn from the current valuation/CVR) → `IssueClientInvoice` reduces the amount available → `RecordCashCallReceipt` increments `Project.CashCallTotal`. Reuse `ValuationClaim`'s `Draft → Preapproved → Confirmed` lifecycle as the invoice engine underneath; `CashCall` is the project-level ledger view the directors asked for.

### 4.8 Document upload + audit trail (any request)

"Optional document upload can happen on any request to store things, like drawings, with an audit trail." No blob storage exists. Plan:

- Provision an **Azure Blob** container (add to `infra/azure-prod-setup-v2.sh`); add a `BlobServiceClient` helper alongside the existing storage-queue client.
- New generic model keyed by the record-agnostic `RecordType`/`RecordId` (so it serves Requests, VOQs, VOs and bid packages uniformly, matching the Linkable-Record seam):

```
RecordDocument: RecordDocumentId, RecordType, RecordId, ProjectId,
                FileName, ContentType, BlobPath, SizeBytes,
                UploadedByEmail, UploadedAt
RecordDocumentAudit: AuditId, RecordDocumentId, Action (Uploaded|Downloaded|Deleted),
                     ActorEmail, At
```

- Commands: `UploadRecordDocument`, `ListRecordDocuments`, `GetRecordDocument` (streams from blob + writes an audit row), `DeleteRecordDocument`. This also satisfies the deferred "Documents section" in the Bid-Package spec.

---

## 5. State machines (summary)

```
REQUEST:   General ─promote→ RFI ─enableRfq→ RFI+RFQ ─createVoq→ (linked VOQ)
                                   (promote emails architect from Client account)

VOQ:       Draft ─invite→ Inviting ─quotes in→ Tendering ─pick winner→ Selected ─approve→ Approved
                                                                          │
                                                                          └→ (on approve) creates VARIATION ORDER

VO:        Approved ─issue→ Issued ─(rare)→ Cancelled
             └ on approve: writes ValuationLineItem(Variation) + QsAccrual/CommittedAmount + CvrSnapshot

CASH CALL: Requested ─invoice→ Invoiced ─payment in→ Received (+= Project.CashCallTotal)
```

---

## 6. CQRS surface to add

Following the existing contract → handler → auth → validation → endpoint pattern, self-registered per feature.

**Requests** (`contracts/Requests`, `api/Features/Requests`)
- `PromoteRequestToRfi(RequestId)` → emails architect (Client account) via `SendRequestDocument`.
- `EnableRfqOnRequest(RequestId)`.

**Variations** (new `contracts/Variations`, `api/Features/Variations`)
- Commands: `CreateVoqFromRfq(RequestId)`, `AddBidPackageToVoq(VoqId, …)`, `SelectVoqTender(VoqId, BidPackageId, SubcontractorId)`, `ApproveVariationOrderQuote(VoqId)` (→ creates VO + valuation lines + budget/CVR writes), `RejectVariationOrderQuote(VoqId)`, `IssueVariationOrder(VoId)`, `CancelVariationOrder(VoId)`.
- Queries: `GetVoqById`, `ListVoqsForProject`, `GetVariationOrderById`, `ListVariationOrdersForProject`.

**Clients** (new `contracts/Clients`, `api/Features/Clients`)
- `CreateClient`, `UpdateClientArchitect`, `LinkProjectToClient`, `GetClient`, `ListClients`.

**Cash Call** (new `contracts/CashCalls`, `api/Features/CashCalls`)
- `CreateCashCall(ProjectId, PeriodMonth, ValuationClaimId?)`, `IssueClientInvoice(CashCallId)`, `RecordCashCallReceipt(CashCallId, AmountReceived)`, `ListCashCallsForProject`, `GetProjectCashCallTotal`.

**Documents** (new `contracts/RecordDocuments`, `api/Features/RecordDocuments`)
- `UploadRecordDocument`, `ListRecordDocuments`, `GetRecordDocument`, `DeleteRecordDocument`.

**RecordLinks / Agents** — add `RecordType.VariationOrderQuote` and `RecordType.VariationOrder`; add `VoqLinkProvider` / `VariationOrderLinkProvider` implementing `ILinkableRecordProvider` so both are triage-linkable and agent-eligible (Part A of the Bid-Package spec).

**"Events" without a bus.** There is no domain-event framework. Cross-entity effects (VO approval → valuation lines → budget → CVR) run **inside the command handler transaction**; anything out-of-band (emails, invoices) is enqueued as a **mailbox/storage-queue action**, exactly like `SendRequestDocument` today. Do not introduce an event bus for this feature.

---

## 7. Migrations

EF Core migrations, timestamp-named (`YYYYMMDDHHMMSS_Description`), auto-applied on startup:

1. `AddGeneralRequestTypeAndRfqFlag` — `Request.HasRfq`, `Request.ClientId` (the `General` enum value needs no schema change; it's an int).
2. `AddClientAccounts` — `Client` table; `Project.ClientId`.
3. `AddVariationOrderQuotes` — `VariationOrderQuote` table; `BidPackage.VariationOrderQuoteId`.
4. `AddVariationOrders` — `VariationOrder` table.
5. `AddCostCodeCommittedAmount` — `CostCodeBudget.CommittedAmount`.
6. `AddCashCalls` — `CashCall` table; `Project.CashCallTotal`.
7. `AddRecordDocuments` — `RecordDocument` + `RecordDocumentAudit` tables.

Each also adds the `DbSet`s to `api/Data/JpmsContext.cs`. Decimal precision inherits the global `(18,4)` convention.

---

## 8. Email & document wiring

- **RFI-to-architect** reuses the request-document pipeline (`RequestDocumentBuilder`/`Renderer`, `SendRequestDocument` mailbox action, Graph `Mail.Send`). Only the recipient resolution changes (Client account first).
- **Bid Package invites** use the invite pipeline from the Bid-Package spec (ACS or Graph, per that spec).
- **Client invoices / cash calls** render via the same MigraDoc approach; sending is a queued action. No new email infrastructure.

---

## 9. New infrastructure required

Only two genuinely new platform pieces:

1. **Azure Blob Storage container** for `RecordDocument` (drawings/attachments). Add to `infra/azure-prod-setup-v2.sh`; inject a `BlobServiceClient` (reuse the storage account already backing the queues).
2. Nothing else — SQL, CQRS, ACS email, Graph, queues and PDF rendering all already exist.

---

## 10. Reconciliation with in-flight specs

- **`Valuation-Report-Tab-Spec.md`** — this plan **consumes** its `ValuationLineItem`/`ValuationClaim`/`ClaimLine` model as the "Add VO to CVR" and cash-call targets. It **resolves that spec's open decision D5** by adding `CostCodeBudget.CommittedAmount`. No collision with the legacy `Valuation` entity (left for CVR/cashflow, per its decision 3).
- **`Bid-Package-Invite-and-Agent-System-Spec.md`** — VOQ becomes the **parent** of bid packages via `VariationOrderQuoteId`; `RecordType`/agent-by-type (Part A) is extended with VOQ and VO; the deferred "Documents section" is delivered by §4.8. The invite question set and AI-draft stub are unchanged.
- **`Linkable-Record-Container-Plan.md`** — VOQ and VO get `ILinkableRecordProvider`s so they are triage-linkable and agent-eligible with no new link machinery.

---

## 11. Open decisions (need Nigel's call)

- **D-A — Client account shape.** Standalone `Client` entity (recommended, matches "client account" language and lets a client own many projects) vs. reusing the unified directory (`Subcontractor` with `DirectoryCategory.Client/Architect`). Recommendation: standalone `Client`, cross-referenced to the directory for the address-book modal.
- **D-B — VO ↔ valuation line coupling.** On VO approval, auto-generate the `ValuationLineItem` variation rows (recommended, keeps CVR/PVR/CVR-single-source guarantee) vs. QS enters them manually and the VO just links. Recommendation: auto-generate, QS can edit after.
- **D-C — Cash Call vs ValuationClaim.** New `CashCall` project-ledger entity over the top of `ValuationClaim` (recommended) vs. extend `ValuationClaim` with received-amounts and derive a project total. Recommendation: thin `CashCall` + `Project.CashCallTotal`, claim stays the invoice engine.
- **D-D — "General" as a type vs a status.** Add `RequestType.General` (recommended — matches "default *state* of any request" while keeping type promotion) vs. a separate `IsTriaged` flag. Recommendation: `RequestType.General = 7`.
- **D-E — Numbering.** VOQ (`VOQ-NNNN`) and VO (`V##`, matching the By France register `V01…V73`) reference schemes — confirm VO uses the `V##` form the valuation report already expects for `VariationRef`.

---

## 12. Phasing

1. **Phase 1 — Request ladder + Client account.** `General` default, `PromoteRequestToRfi` (architect email from Client), `EnableRfqOnRequest`; `Client` entity + `Project.ClientId`. Migrations 1–2.
2. **Phase 2 — VOQ + bid-package linkage.** `VariationOrderQuote`, `BidPackage.VariationOrderQuoteId`, create-from-RFQ, invite/tender/select reusing existing procurement. Migration 3. Depends on the Bid-Package spec Part A/B landing.
3. **Phase 3 — VO + CVR write-through.** `VariationOrder`, `ApproveVariationOrderQuote` → VO + `ValuationLineItem(Variation)` + `CommittedAmount` + `CvrSnapshot`. Migrations 4–5.
4. **Phase 4 — Cash Call.** `CashCall` + `Project.CashCallTotal`, monthly create/invoice/receipt. Migration 6.
5. **Phase 5 — Document upload.** Blob container + `RecordDocument`/audit across all record types. Migration 7.

Each phase is independently shippable and leaves the system green.

---

## 13. Verification (per phase)

Unit tests on the maths and the transitions: VO approval produces the exact valuation lines and budget/CVR deltas; cash-call receipts sum to `Project.CashCallTotal`; the By France figures still reconcile after VO write-through (Revised Contract Sum £2,015,640.33, etc., per the Valuation-Report spec). Build + render check of any new tabs. For the VO→CVR→cash-call chain (highest blast radius), a scripted end-to-end fixture is recommended before sign-off.
