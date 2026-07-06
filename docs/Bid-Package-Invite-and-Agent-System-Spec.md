# Bid Package Invite & Agent‑System Change — Design Spec

Status: **Draft for review** · Author: assisted draft · Date: 2026‑06‑30

> **Captured requirements — bid-package detail view & invite content (2026‑06‑30, from client review):**
>
> *Detail view (UI built this round):* the bid-package page now shows **Invited to tender** (with add via a searchable/filterable **directory modal**, and remove), **Line items** (grouped by trade, editable), a **Documents** section (placeholder — see below), **Related emails** (emails tagged to the package, read live), and a **Generate using AI** button (confirm → review → apply flow built 2026‑07‑06; generation itself is stubbed behind `RequestAiProposalAsync` — when wired, Claude drafts from related emails + drawings, the draft opens in a review modal, and only accepted lines are merged in; existing content is never changed or removed).
>
> *Address-book modal:* should cover not just subcontractors/suppliers but also **clients and architects** (ties to open decision D3 — a Client/Architect directory is not built yet; the modal currently lists the subcontractor directory).
>
> *Documents section (to build):* manage the **drawings/documents** used for the package — uploaded files **and** cloud-storage links — that can be sent to people **directly or via a cloud link**. Needs a small data model (file/link records) + storage; deferred pending design.
>
> *Creating a bid package invite:*
> - **Do not** capture client details on the package.
> - The invite asks subcontractors the following, **making Jewel's preferences explicit**:
>   - **Deposit:** we prefer **not** to be asked for a deposit upfront — but if one is required for you to submit a bid / do the work, add it.
>   - **Duration:** how long the work will take to complete.
>   - **Availability:** if selected, when you can start.
>   - **Insurances:** confirm you hold all required insurances.
>   - **RAMS:** confirm you have all required RAMS documentation.
>   - **Portfolio:** share any prior work portfolios that help us decide.
> - The package references a **scope of work** and may include **drawing files or cloud links** to drawings.
> - These questions define the invite/RFQ structure and the (future) subcontractor response form; exact output to be confirmed.

This spec covers two related changes:

- **Part A — Foundational change.** Move from *manually assigning* agents to records, to agents being **predefined by record type**. This is the system change to make now, before more document types arrive.
- **Part B — Bid Package Invite (BPI).** The first new record type built on top of Part A: tagged emails → structured extraction by Claude → a stored invite → recipients from the address book → generated draft email + Excel → response intake → award → purchase order → cost‑centre budget. Aligns with the already‑scoped `US‑03‑03` "subcontractor invite".

---

## Part A — Predefined agent applicability

### A.1 How it works today

- Agents are code‑defined singletons (`BidPackagesAgent`, `SchedulingAgent`, `ValuationsAgent`) implementing `IRequestAgent`, collected in `AgentRegistry`.
- `ListAvailableAgents` returns **every** agent as a flat catalogue.
- A human **manually applies** one to a request via the "Apply an agent" dropdown in `AgentWorkspace.razor`, which runs `AssignAgent` and writes a row to the **`RequestAgents`** table.
- Per‑record agent state lives in three tables keyed by `(RequestId, AgentKey)`: `RequestAgents` (status, IsPrimary, StatusMessage), `AgentChatMessages`, `AgentProposals`.
- The **close‑gate** (`AttemptCloseRequest`) blocks closing a request while any *applied* agent is incomplete.

Problem: applicability is manual and per‑record. As new document types are added, every record would need bulk assignment. We want applicability to be intrinsic to the record's type.

### A.2 Target model

> Create a record → the agents for that record type are already available. No assignment step.

The mapping from record type to agent(s) is **declared in code**, derived at read time, never stored as an assignment.

Recommended mapping (one or more agents per record type; designed for many, currently one each):

| Record type | Applicable agent(s) | Status today |
|---|---|---|
| Request (RFI / RFA / RFC / RFQ / RFP) | Requests/RFI agent | agent to be built (see A.6) |
| Notice of Delay / Extension of Time | Scheduling agent | stub exists |
| **Bid Package Invite** | **Bid Packages agent** | stub exists, made real in Part B |
| Valuation record (future) | Valuations agent | stub exists |

### A.3 Concrete code changes

1. **Introduce a record‑type discriminator.** Add `RecordType` (enum: `Request`, `BidPackageInvite`, …) in `contracts/Models`. Requests carry `RecordType.Request`; BPIs carry `RecordType.BidPackageInvite`. (Records remain rows in their own tables; `RecordType` is the key the agent layer maps on.)
2. **Declare applicability on the agent.** Add to `IRequestAgent`:
   ```csharp
   IReadOnlyCollection<RecordType> AppliesTo { get; }
   ```
   Each agent returns the record type(s) it serves. `BidPackagesAgent.AppliesTo => [RecordType.BidPackageInvite]`.
3. **Registry resolves by type.** Add `AgentRegistry.ForRecordType(RecordType type)` returning the matching descriptors.
4. **Replace the catalogue query.** Retire `ListAvailableAgents` (returns all) in favour of `ListApplicableAgents(string recordId)` / `ListApplicableAgents(RecordType type)` returning the predefined set for that record.
5. **Remove manual assignment.** Delete `AssignAgent` and `RemoveRequestAgent` (commands, handlers, validation, authorisation, endpoints) and the "Apply an agent" / "Remove" UI in `AgentWorkspace.razor`.

### A.4 Per‑record agent state & provisioning

Agents still need per‑record state (chat, proposals, completion). Two options:

- **Recommended — keep `RequestAgents` as an auto‑provisioned state row.** It stops being an "assignment" and becomes "this agent's state on this record." Rows are created automatically when a record is created (and lazily back‑filled on first read for existing records) from the type mapping. This preserves all existing chat/proposal/close‑gate wiring with minimal churn. Drop `IsPrimary`/`AssignedByEmail` (no longer meaningful) or repurpose `AssignedByEmail` → `null`/system.
- **Alternative — fully derived, no state table.** Compute applicable agents at query time and move completion state onto the record itself. Cleaner conceptually, larger refactor; not recommended for this step.

Rename the table/entity to reflect the new meaning when convenient (e.g. `RecordAgents`), but a rename is optional.

### A.5 Close‑gate impact

`AttemptCloseRequest` currently gates on *applied* agents. After the change it gates on the **applicable** agents for the record's type (i.e. the auto‑provisioned rows). Behaviour is equivalent; the source of the list changes from "what a human applied" to "what the type defines."

### A.6 The "RFI agent" question

There is no Requests/RFI agent today — only Procurement/Programme/Commercial discipline stubs. Two ways to satisfy "for a request, the RFI agent is applicable":

- **(a)** Add a new `RequestsAgent` (key `requests`/`rfi`) mapped to `RecordType.Request`. Cleanest if requests need their own agent behaviour.
- **(b)** Treat the existing discipline agents as record‑type agents and map `RecordType.Request` to whichever is appropriate.

**Recommendation: (a)** — it keeps the "one record type → its agent" model clean. *Open decision D1.*

### A.7 Data migration

- New records auto‑provision their agent state rows.
- For existing requests: either a one‑off back‑fill migration that inserts the mapped agent row per request, or lazy creation on first read (recommended — no migration risk).
- Any rows for agents no longer mapped to that type can be left in place (ignored) or cleaned up by a data migration.

---

## Part B — Bid Package Invite feature

### B.1 End‑to‑end flow

```
Email arrives → triager tags it "Bid Package Invite" (reuses JPMS tag system)
   → record selected for creation
   → email(s) + drawing files sent to Claude (Bid Packages agent)
   → Claude returns structured BPI: project/cost‑centre, line items (grouped by trade),
     key files to retain, recipient hints
   → BPI record stored; key files copied to Azure blob storage
   → recipients chosen from the address book (multiple subcontractors)
   → draft invite email generated + Claude‑generated Excel attached → saved as Draft
   → invite sent; appears in the new "Bid Package Invites" project tab
   → subcontractor replies tagged "Bid Package Invite Response" → response stored
   → invites managed/retriggered from the tab; replies handled in‑app
   → winner awarded → Purchase Order created → allocated as budgeted cost to cost‑centre
```

### B.2 Record type & project tab

> **DECIDED (D2).** A Bid Package Invite is **not** a request. Requests are inbound, project‑level items. A bid package is an **outbound** artifact Jewel authors and issues to potential subcontractors to tender. The BPI is therefore a **first‑class record in the procurement/bid‑package domain** — it is *not* a `RequestType`. It only *borrows* the email‑tag/triage mechanism to seed creation (an email tagged "Bid Package Invite" is the trigger), but the record, its lifecycle, tables and UI are its own.

- Add `RecordType.BidPackageInvite` (already in Part A) as the agent‑applicability key; the Bid Packages agent applies to it.
- **New project tab "Bid Package Invites", placed immediately after "Requests"** in `jpms/Components/ProjectTabNav.razor` (insert between `requests` and `valuation`). The tab lists bid packages for the project with who has been invited to tender and when, each click‑through to a detail page showing the package, recipients, line items, responses, and management actions (update / retrigger).

### B.3 Tag / triage reuse

- Add a tag value, e.g. `JPMS/BPI-0001`, via the existing `TriageCategories.ForRequest(reference)` mechanism. Triage UI gains a "Bid Package Invite" creation choice alongside RFI.
- Emails carrying the create tag are the ones fed to Claude. A second tag, **"Bid Package Invite Response"**, classifies inbound replies so they store as responses against the invite (B.11).

### B.4 Claude structured extraction (makes the Bid Packages agent real)

This is where Part A pays off: the BPI record’s applicable agent is the **Bid Packages agent**, and its `AnalyseAsync` becomes a real Claude call instead of the stub.

- **Wire a real LLM client.** Nothing calls Claude today. Add an `ILlmClient` (Anthropic Messages API) in the api/worker, configured via Key Vault. `RequestContextAssembler.ToPromptText()` already assembles record context for hand‑off.
- **Structured output.** Define a JSON schema for the extraction result: `projectId`, `costCentreCode`, `lineItems[] {description, unit, quantity, trade/speciality}`, `recipientHints[]`, `keyFiles[]`. Persist as the agent’s `AgentProposal.StructuredJson` (the proposal/accept‑reject UI already exists), so a human confirms before the invite is created.
- **Drawing files to Claude.** The agent passes drawing attachments (PDF/image) to Claude alongside the email text.

### B.5 Project & cost‑centre identification

- Line items belong to a specific **project** and **cost‑centre** (`CostCenter` + `CostCodeBudget` already exist).
- Resolve the project by the most common project‑name/reference across the tagged email(s); pass candidate projects to Claude in the call and let it return the `projectId`. The web app supplies the candidate list (it has the data) — Claude disambiguates.
- Cost‑centre resolved similarly (Claude returns a code from the supplied `CostCenters` list).

### B.6 Address book (recipients)

- **Subcontractors already exist** as a directory (`AddSubcontractorToDirectory`, `ListSubcontractors`; fields: company, primary trade, contact name/email/phone, CIS, compliance docs). Reuse it as the recipient source — select **multiple** subcontractors as invitees.
- **Clients do not exist yet.** Add a client directory (mirror the subcontractor pattern: `Client` model + `ClientEntity` + add/list/update contracts) or generalise into one "address book" with a party‑type field. *Open decision D3.*
- Recipients can be pre‑filtered by trade/speciality matching the package’s line‑item groupings.

### B.7 New data model

New entities (companion to the reused request record, or first‑class per D2):

- **BidPackageInvite** — `InviteId, RecordId/RequestId, ProjectId, CostCentreCode, Title, Status (Draft|Sent|ResponsesIn|Awarded|Closed), CreatedAt, CreatedByEmail`. (Note an existing `BidPackage`/`Quote`/`WorkOrder` set exists — *Open decision D4: extend the existing `BidPackage` entities vs. introduce BPI alongside them.* Strong preference to extend rather than duplicate.)
- **BidPackageLineItem** — `LineItemId, InviteId, Description, Unit, Quantity, Trade/Speciality, SortOrder`.
- **BidPackageRecipient** — `RecipientId, InviteId, SubcontractorId, InvitedAt, Status (Invited|Responded|Declined|Won)`.
- **BidPackageResponse** — `ResponseId, InviteId, SubcontractorId, ReceivedAt, TotalValue, Comment, SourceEmailId`; with per‑line pricing `BidPackageResponseLine (ResponseId, LineItemId, UnitPrice, Comment)`.
- **BidPackageFile** — `FileId, InviteId, FileName, BlobUri, Kind, IsKeyFile` (B.8).
- Reuse `WorkOrder`/Purchase Order for the award (B.12).

### B.8 Drawing files & key‑file storage (Azure blob)

- **No blob storage client is wired today** (only Azure Storage Queues). Add `Azure.Storage.Blobs` with a `BlobServiceClient` against the existing storage account; create/confirm a container for bid‑package files.
- When Claude flags a file as a **key file**, the backend copies it from the source email into the blob container and records a `BidPackageFile` row with `IsKeyFile = true`. Files are then served/attached from blob, not re‑fetched from the mailbox.

### B.9 Draft email generation

- The invite email is **generated, populated, and saved as a Draft** (not auto‑sent). Today outbound is worker‑rendered PDF via Graph with no draft concept — add a draft state (a `RequestMessage` with `Direction=Outbound, SentStatus=Pending`, or a Graph draft) editable before send.
- On send, reuse the worker/Graph `SendMail` path; record an outbound `RequestMessage`; move invite to `Sent`.

### B.10 Excel line‑item workbook

- The invite carries a **Claude‑generated Excel** of the package’s line items: columns for description, unit, quantity, **unit price** and **total** (for the subcontractor to complete), a **comment** field, with rows **grouped by trade/speciality** (electrician, plumber, …).
- **No Excel generation exists today** (PDF only, via MigraDoc). Add a workbook generator (ClosedXML recommended). Build the model from `BidPackageLineItem` rows.

### B.11 Response intake & management

- Inbound emails tagged **"Bid Package Invite Response"** store a `BidPackageResponse` (+ lines) against the invite, linked by recipient. Parsing the returned Excel/price can again use the Bid Packages agent.
- The invite detail page (new tab) is **click‑through** and lets staff **update and retrigger** invites, view who was invited and when, and compare responses (the existing `QuoteComparisonGrid` pattern can be reused/extended).

### B.12 In‑app reply & Purchase Order

- Use the in‑app email functionality to **reply professionally** to responses from within the app (extends B.9’s outbound path to threaded replies — `RequestMessage` already carries `EmailMessageId`/`InReplyTo`/`ConversationId`).
- **Award → Purchase Order.** Reuse `AwardBidPackage` → `WorkOrder` as the PO (or add an explicit `PurchaseOrder`). The winning recipient is marked `Won`.

### B.13 PO → cost‑centre budget allocation

- On award, **allocate the PO value as budgeted/committed cost** to the associated cost‑centre via `CostCodeBudget` (`SetCostCodeBudget`). Decide whether this writes to `SpentAmount`, or a new **Committed** column is added for pre‑actual commitments. *Open decision D5 — recommend adding a `CommittedAmount` so committed ≠ spent.*

---

## Phasing & sequencing

1. **Phase 0 — Foundational agent change (Part A). ✅ Implemented 2026‑06‑30.** Added `RecordType` + `IRequestAgent.AppliesTo` + `AgentRegistry.ForRecordType`; new non‑blocking `RequestsAgent` for `RecordType.Request`; `AgentProvisioning` lazily materialises a record's predefined agent state (no DB migration — reuses `RequestAgents`); `ListRequestAgents` and the close‑gate now provision + read type‑derived agents; deleted `AssignAgent`/`RemoveRequestAgent`/`ListAvailableAgents` end‑to‑end (contracts, endpoints, handlers, routes, client desk, read model) and removed the "Apply an agent"/Remove UI. *Note: built static‑verified only — run `dotnet build` to confirm in CI.*
2. **Phase 1 — BPI record + tab + triage tag.** *In progress.* ✅ "Bid Package Invites" tab added after Requests (`ProjectBidPackageInvites.razor`, lists the project's packages). ✅ Data layer: `BidPackageRecipient` + `BidPackageLineItem` entities/models/mapping, contracts (`InviteSubcontractorsToBidPackage`, `SetBidPackageLineItems`, `ListBidPackageRecipients`, `ListBidPackageLineItems`), API handlers/endpoints/validation/auth, DI + client routes, and a committed EF migration `20260630120000_AddBidPackageInvites` (+ updated `JpmsContextModelSnapshot`) that the app's startup `MigrateAsync()` auto-applies on deploy — no manual CLI step. (`api/Migrations/manual-sql/AddBidPackageInvites.sql` is now reference-only; don't run it by hand given auto-migrate.) ✅ Invite UI: bid-package detail page (`ProjectBidPackageInviteDetail.razor`) — invite subcontractors from the directory (multi-select), shows who's invited and when + status, and line items grouped by trade; list rows link through to it. ✅ Line-item editor: inline edit mode on the detail page (add/edit/remove rows with trade, description, unit, qty; saves via `SetBidPackageLineItems`). ✅ Email linking: `BidPackageInviteLinkProvider` plugs BPIs into the repo's generic `RecordLinks` system (added externally in commit `a2c15fe`), so a tagged email can be linked to a bid package and read back live by tag. ✅ Email→BPI creation: `CreateBidPackageFromMessage` (command + handler + endpoint `POST /api/mailbox/message/create-bid-package` + validation/auth + DI + client route). Creates a Draft package and delegates email tagging to the shared `LinkMessageToRecord` handler (so the tag matches the provider). ⏳ Remaining: a "Create bid package" trigger in the triage UI (left to coordinate with the in‑flight triage refactor), and award→PO (deferred toward Phase 2 with AI). Bid Packages agent auto‑applies via Phase 0.

> **Concurrency note (2026‑06‑30):** the repo changed during this work — `RecordType` and the `RecordLinks` provider layer were committed after the session's starting point by an external actor. All changes here are consistent with it (single `RecordType` enum; BPI plugged into `RecordLinks` the sanctioned way).
3. **Phase 2 — LLM extraction. ⏸ On hold (deferred by request, 2026‑06‑30).** Wire `ILlmClient`, real `BidPackagesAgent.AnalyseAsync`, structured proposal (project/cost‑centre/line items), drawing‑file input, human accept → BPI populated. Foundations already in place: the Bid Packages agent auto‑applies to BPIs (Phase 0), and a BPI reads its emails live by tag (link provider). Still needs decision D6 (LLM provider + credentials).
4. **Phase 3 — Address book + recipients.** Client directory (or generalise), multi‑subcontractor selection, trade filtering.
5. **Phase 4 — Outbound: draft email + Excel + blob.** Draft generation, ClosedXML workbook, blob storage of key files, send path.
6. **Phase 5 — Responses + management.** Response tag/intake, invite detail page, compare, update/retrigger.
7. **Phase 6 — Award → PO → budget.** In‑app reply, PO creation, cost‑centre allocation.

## Open decisions

- **D1.** Add a dedicated Requests/RFI agent, or map Request records to an existing discipline agent? (A.6)
- **D2.** Is a BPI a `Request` subtype reusing the request pipeline, or a first‑class record with its own tables? (B.2)
- **D3. DECIDED (2026‑06‑30):** one **unified company directory** with a `DirectoryCategory` property (Subcontractor / Client / Architect / Supplier / Other, extensible). Built by **extending the existing `SubcontractorEntity`** (added `Category` + master-sheet columns: MobileNumber, Town, County, Website, Pli, PliExpiry) rather than forking a new table — keeps existing subcontractors, compliance docs and bid-package recipient links intact. Migration `20260630130000_AddCompanyDirectory` adds the columns only (auto-applied on deploy). The **246-record seed is NOT committed** — it's third-party contact data, so it lives in **`scripts/seed-subcontractors.sql`** (git-ignored) and is loaded manually with `sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i scripts/seed-subcontractors.sql` (idempotent, after the migration has added the columns). ⏳ Next: directory page category filter + search-all + add Client/Architect entries via UI; bid-invite modal to filter to Subcontractor/Supplier.
- **D4.** Extend the existing `BidPackage`/`Quote`/`WorkOrder` entities, or introduce BPI entities alongside them? (B.7)
- **D5.** Allocate PO value to `SpentAmount`, or add a `CommittedAmount` column? (B.13)
- **D6.** Confirm the Anthropic Claude API as the LLM, and where credentials live (Key Vault). (B.4)
