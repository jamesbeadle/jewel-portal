# Pathway split — Client / Subcontractor / Internal: platform flow analysis

Status: **Proposed — extends and partially supersedes `docs/Communication-Buckets-Plan.md`.**
Date: 2026-07-22.

`Communication-Buckets-Plan.md` designed the *tag mechanics* (bucket categories, stamping,
backfill). This document takes the wider brief: triage becomes a **first-stage router** that
forces every communication down one of three pathways, the side panel is restructured into six
collapsible folders, and a **global audit trail** records every triage event and every
portal-drafted communication. Where the two documents disagree, this one wins; the mechanics
sections of the buckets plan remain the implementation reference for slices that survive
unchanged (they are cross-referenced below).

---

## 0. The governing principle — the client wall

The review discussion sharpened the requirement. The risk being protected against is precisely:

> **A client must never be able to read subcontractor or internal correspondence.**

That is narrower — and stronger — than "no communications ever mix". It means the invariant that
must be *unbreakable* is the wall between **Client** and **everything else**, while
Subcontractor ↔ Internal is an organisational preference, not a safety boundary. The design
below therefore has two tiers:

1. **The wall (hard, structural):** a thread can never carry the Client pathway *and* a
   non-Client pathway. Every client-visible surface reads **only** Client-pathway material, by
   construction — not by filtering discipline. Server-side guards reject any action that would
   breach it; there is no override.
2. **The lanes (soft, default-one):** within the non-client side, one pathway per thread is the
   default (first tag sets it), but a user may deliberately cross-file a thread as both
   Subcontractor and Internal with a warning. Both the default stamp and the override are audit
   events.

This resolves the tension in the brief between "no communications should ever mix" and "if the
user wanted it on another pathway as an option they should be able to do that also": the option
exists everywhere except across the wall.

A necessary honesty note on scope: the portal's segregation is **tag-based, not mailbox-ACL
based**. Anyone with access to the shared `projects@jewelbb.co.uk` mailbox in Outlook sees
everything, as today. The wall is enforced on every *portal* surface a client (or
client-visible artefact, e.g. a request activity trail or a PDF) can reach. If clients are ever
given direct portal logins beyond today's `Role.Client` (which currently has no project
workspace access at all — `DesktopNavigation.CanSeeProjects` excludes it), the wall is the
mechanism that makes that safe.

---

## 1. What exists today — the inventory this analysis is built on

### 1.1 Triage and tagging (the machinery the pathways sit on)

- Un-triaged mail *is* the Inbox: the queue is a live Graph read of messages without the
  `JPMS` marker category (`MailboxGraphClient.ListInboxAsync`). No email is ever moved or
  copied; triage stamps Outlook **categories** and the DB holds only workflow records.
- Tag shapes: `JPMS` marker, `JPMS/Discarded`, and record tags `JPMS/<TagReference>` —
  `JPMS/JBB-2026-001-RFI-012`, `JPMS/CC-<proj>-<code>`, `JPMS/BPI-0001`, `JPMS/TODO-0001`,
  `JPMS/SCH-<proj>`, `JPMS/VO-…`, `JPMS/VOQ-…`, `JPMS/LAD-…` (provider `ReferencePrefixes`
  in `RecordProviderRegistry` keep the flat namespace collision-free).
- Tags are applied **thread-wide** (`RecordThreadTagger`: anchor verified by read-back,
  siblings best-effort, Sent Items included, queue sweep inherits tags onto late replies).
- Triage actions today (TriageQueue.razor → handlers): link to record(s)
  (`LinkMessageToRecordHandler`, multi-select), create request
  (`CreateRequestFromMessageHandler`), create bid package
  (`CreateBidPackageFromMessage`), create to-do(s) (`CreateTodoItemsFromMessageHandler`),
  reply-in-thread (creates a General request), discard / restore / remove tag, sync thread
  tags, and the advisory AI `RecommendTriageActionHandler`.
- Record types (`RecordType`): Request, BidPackageInvite, CostCentre, Scheduling, Todo, Lad,
  Variation, VariationQuote.

### 1.2 Audit infrastructure (the gap)

There is **no global audit trail**. What exists is feature-scoped:

- `ValuationInvoiceEventEntity` + `ValuationInvoiceAuditTrail.Append(...)` — the one true
  typed event log, written in-transaction by every valuation-invoice command. This is the
  pattern to generalise.
- `RequestMessageEntity` — the request activity history; the worker writes an Outbound/Pending
  row when it drafts an RFI/NOD/EOT document. **Client-facing**: To + CC only, BCC only as a
  count.
- Everything else is implicit: a triage decision's only durable trace is the Outlook category
  itself. Nobody records *who* linked, discarded, or removed a tag, *when*, or what the AI
  recommended versus what the human did. Drafted bid-package invites and work-order emails
  leave no DB trace at all beyond the record's status.

### 1.3 Navigation today

Sidebar (`SideNav.razor`, catalog in `DesktopNavigation.cs` / `ProjectSections.cs`): Home; then
the project workspace (picker + three always-open headed blocks — Project Management,
Operations, Financials — plus Project settings); then a flat "Company" list (Triage, Financial
Summary, Cash Summary, Xero, Cost codes & Rates, Workers, Directory, Clients, Architects).
The sidebar collapses to an icon rail, but **blocks have no per-header collapse** — that
mechanism does not exist yet. `WorkspaceSections.cs` gives Xero and Cost codes & Rates in-page
sibling tabs. Nav visibility mirrors API authorisation but the API is the enforcement.

---

## 2. The three pathways and triage as the router

### 2.1 The flow, restated through the new lens

Today's triage panel is **action-first**: the triager picks an action (create request, create
bid package, create todo, link, discard). The new model makes it **pathway-first**: the first
decision on any email is *who is this correspondence with?* — and the pathway chosen determines
which actions are offered:

```
                        ┌─────────────────────────────┐
        Inbox email ──▶ │  TRIAGE (the router)        │──▶ Discard (no pathway)
                        └──────┬───────┬───────┬──────┘
                               ▼       ▼       ▼
                           CLIENT   SUBCON   INTERNAL
                               │       │       │
     default container:    Request   Bid     Todo
                          (General)  Package (project or
                               │     Invite  company-wide)
     official actions:    RFI/NOD/EOT  │       │
                          VOQ → VO   Work    Labour /
                          Valuation  Order   assignments
                          snapshots  award
```

- **Client pathway.** The default container is the **General request** (`RequestType.General`,
  `REQ-####`) — exactly the "low level, unofficial" container the brief describes, sitting
  above the official Request family (RFI, RFA, RFC, RFQ, RFP, NOD, EOT) that promotion mints
  (`PromoteRequestToRfi` and the ladder from the Entity-Refactor plan). Nothing changes in the
  promotion machinery; what changes is that creating that container also stamps `JPMS/Client`
  thread-wide and writes an audit event. Cost-centre correspondence that is valuation-side, VO
  and VOQ threads are Client (per the buckets plan mapping).
- **Subcontractor pathway.** Bid package invites are created and sent here (drafted by
  `PrepareBidPackageInviteDraftHandler`, BCC-only recipients, tagged `JPMS/BPI-####` +
  `JPMS/Subcontractor`). Work-order email drafts and subcontract-side cost-centre mail live
  here too.
- **Internal pathway.** The project and company to-do list (`TodoItem`, `ProjectId == ""` for
  company-wide), assigned by role. A thread whose only link is a to-do is Internal by default —
  but a to-do raised *from* a client email is pathway-neutral and leaves the thread Client
  (the buckets plan's "Todo = neutral" rule survives; it is precisely the "copy, don't share"
  pattern the wall needs).
- **Discard** remains pathway-less (`JPMS/Discarded`), and restore returns to the queue.

### 2.2 What "forcing" means in the UI

The triage detail pane gains a **pathway selector as the first control** — three segmented
options (Client / Subcontractor / Internal). Selecting one:

1. filters the action list to that pathway's actions (Client → create/link request, link VO/VOQ,
   link valuation-side cost centre, reply-in-thread; Subcontractor → create/link bid package,
   link work order, link subcontract cost centre; Internal → create to-do(s), link scheduling/
   programme, link LAD prep material);
2. shows the consequence line from the buckets plan ("This thread will be filed under
   **Client**");
3. pre-seeds the AI recommendation: `RecommendTriageActionHandler` already classifies sender
   role (client/architect/subcontractor/internal) — its output should now lead with a suggested
   pathway, and the accept/override becomes an audit-visible pair (recommended vs chosen).

The pathway tag itself is still **derived, not free-standing**: it is stamped when the first
record link/create happens (per the buckets plan mechanics), so an email can never carry a
pathway with no record — the existing rule "removing the last record tag also removes the
bucket tag and the marker" survives unchanged. The selector is a lens on the action list, not a
separate write.

Cross-project note: `Scheduling` (Programme) and `Lad` links map to **Client** in the buckets
plan (programme correspondence is client/architect-facing). Under the pathway-first UI those
links appear in the Client action set; the ⚠ defaults in the buckets plan mapping table remain
the place to flip any of these one-liners.

### 2.3 The two-tier mixing rule (supersedes the buckets plan's single invariant)

- `LinkMessageToRecordHandler` (the single choke point — Assign is an adapter over it) resolves
  the candidate pathway from the record type before tagging:
  - Thread has no pathway → stamp it.
  - Same pathway → no-op.
  - **Client ↔ non-Client conflict → reject, always.** Actionable message ("This thread is
    filed under Client; BPI-0004 would file it under Subcontractor. Start a new thread or
    forward the relevant content."), no override parameter exists on the wall.
  - **Subcontractor ↔ Internal conflict → reject by default; accept with an explicit
    `AllowCrossPathway` flag** the UI sets only after a warning dialog. Both tags then coexist
    and the override is an audit event with the actor recorded.
- The Tagged view's "add another tag" picker shows each candidate record's pathway and visually
  separates the wall (client records simply not offered on a non-client thread, and vice versa)
  from the lane (offered with a warning glyph).
- The **conflict report** from the buckets plan stays, now with severity: a thread carrying
  Client + non-Client (should be impossible; anything found is a wall breach to fix urgently)
  versus Subcontractor + Internal (informational — lists deliberate overrides alongside any
  backfill leftovers).

---

## 3. Data segregation — where the wall is actually enforced

Tagging alone does not stop a client seeing subcontractor mail; *surfaces* do. Every place
correspondence or correspondence-derived content can reach a client must read Client-pathway
material only. The enforcement points, from the inventory:

| Surface | Today | Under the wall |
|---|---|---|
| `RequestMessageEntity` activity trail (`Visibility = Shared`) on `ProjectRequestDetail` | Shared rows list To+CC, BCC as count only | Unchanged mechanics; requests are Client-pathway by construction, so the trail is inside the wall already. Guard: reply-in-thread and `PrepareRequestReplyDraftHandler` must refuse to operate on a non-Client thread (belt-and-braces — they should never meet one). |
| `ListProjectCommunications` (Communications tab) | Rolls up **every** record type's mail | Internal-staff surface — keeps showing everything, gains the pathway segmented control (buckets plan). If any client-visible export/report is ever built on it, that consumer must pass `Bucket=Client` server-side, not client-side. |
| Request document PDFs (`RequestDocumentBuilder`) | To + CC only, BCC structurally absent | Unchanged — already wall-safe. |
| Progress reports (client-facing PDF) | Assembled from `ProgressUpdate`s, no mail content | No change; progress photos/narrative are not correspondence. |
| Bid package invites | To = mailbox itself, recipients BCC (subs can't see each other) | Unchanged — and now also structurally incapable of touching a Client thread. |
| `/portal` (external subcontractor landing) | Subcontractor's own record only | If a client portal is ever added, its mail reads must be server-side filtered to `JPMS/Client` **and** to that client's own projects. The wall makes this a one-clause filter. |
| Valuation report snapshots | Immutable line-level frozen copies | These are the client-facing financial statements — surfaced on the Client side of the nav (§5). They contain no correspondence, so no leak vector; the pathway framing is organisational. |

The key structural win: because the wall is a **tag invariant plus a handler-level rejection**,
client-visible surfaces don't each need their own filtering logic to be correct — they need one
exact-match Graph clause (`categories/any(c: c eq 'JPMS/Client')`), which Graph handles cheaply
(same trick as the `JPMS` marker; no prefix scans).

---

## 4. The audit trail

### 4.1 Requirement, restated

Two distinct needs from the brief, one mechanism:

1. **Findability** — "all communications drafted by the portal should be recorded … so emails
   can be quickly found in Outlook."
2. **Traceability** — "all pathways will record the triage event … this audit trail should be
   all audit events."

### 4.2 Design: one append-only `AuditEvents` table

Generalise the `ValuationInvoiceAuditTrail` pattern (append into the change tracker, the owning
handler saves, so event + state change commit in one transaction):

```
AuditEventEntity
  AuditEventId       (pk)
  OccurredAt         (utc)
  ActorEmail         (the signed-in triager/PM; "worker" for the action worker)
  EventType          (int enum — see vocabulary below)
  Pathway            (int enum: None/Client/Subcontractor/Internal)
  ProjectId          (nullable — company-wide events have none)
  RecordType         (nullable int — the existing RecordType enum)
  RecordId           (nullable)
  RecordReference    (denormalised display ref, e.g. "RFI-012", "BPI-0004", "TODO-0113")
  ConversationId     (nullable — Graph conversation)
  EmailMessageId     (nullable — Graph message id of the anchor/draft)
  InternetMessageId  (nullable — RFC id; survives mailbox moves, best Outlook search key)
  WebLink            (nullable — Graph webLink; one click opens the message in Outlook)
  Detail             (≤1024, human sentence: "Linked to RFI-012 and CC-JBB-2026-001-00042")
  RecommendedAction  (nullable — what the AI suggested, for the recommend-vs-did loop)
```

Event vocabulary (initial): `EmailTriaged` (pathway chosen via first link/create),
`RecordLinked`, `RecordCreatedFromEmail`, `TagRemoved`, `Discarded`, `Restored`,
`CrossPathwayOverride` (Sub↔Internal, actor + both pathways), `WallRejected` (attempted
Client↔non-Client link — recording refusals is cheap and makes the conflict report largely a
query), `DraftCreated` (every `CreateDraftAsync`/`CreateReplyDraftAsync` call: request
documents, request replies, bid package invites, work-order emails), `ThreadSwept` (optional,
low-value — see trade-offs), `BackfillStamped`.

Writers — the choke points are already narrow, which is what makes this cheap:

- `LinkMessageToRecordHandler` — `EmailTriaged`/`RecordLinked`/`CrossPathwayOverride`/`WallRejected`.
- `CreateRequestFromMessageHandler`, `CreateBidPackageFromMessage`,
  `CreateTodoItemsFromMessageHandler`, `ReplyInThreadFromMessageHandler` — `RecordCreatedFromEmail`.
- `DiscardMessageHandler` / `RestoreMessageHandler` / `RemoveTagFromMessageHandler` —
  `Discarded`/`Restored`/`TagRemoved`.
- `MailboxActionWorker.SendRequestDocumentAsync`, `PrepareBidPackageInviteDraftHandler`,
  `PrepareWorkOrderEmailDraftHandler`, `PrepareRequestReplyDraftHandler` — `DraftCreated`,
  capturing the draft's Graph id + webLink at creation time. This is the findability half: the
  audit register becomes the index into Outlook for everything the portal ever drafted.
- The backfill endpoint — `BackfillStamped` per conversation.

Non-writers, deliberately: the queue sweep's tag inheritance (`SweepQueuePageAsync`) fires on
every queue listing and would flood the log with machine events; it stays out unless a real
investigation need appears (`ThreadSwept` is reserved in the vocabulary but off by default).

### 4.3 Surfacing

A single **Audit** register page (company-scoped, filterable by pathway / project / event type /
actor / date, newest first), each row deep-linking to the record and — via `WebLink` — to the
email in Outlook. Per the new sidebar it lives in the **Internal** folder (it is an internal
oversight tool; audit rows about client-pathway events are *about* the wall, not inside it —
the register itself is never client-visible). Feature slice: `api/Features/Audit/` with
`AuditTrail.Append(...)` (mirroring `ValuationInvoiceAuditTrail`), `ListAuditEvents` query with
cursor paging, `jpms/Pages/AuditTrail.razor`.

Existing logs are not migrated: `ValuationInvoiceEvents` stays as-is (financial approvals have
their own richer shape — before/after amounts); `RequestMessageEntity` stays as-is (it is a
client-facing correspondence trail, a different animal). The audit register is additive.

---

## 5. Feature-by-feature impact through the pathway lens

| Feature (files) | Pathway home | Impact |
|---|---|---|
| **Triage queue** (`TriageQueue.razor`, mailbox handlers) | Router (nav: Internal folder) | Pathway-first action panel (§2.2); wall/lane guards (§2.3); every action writes audit events (§4). Queue view itself unchanged — untagged mail has no pathway by definition. |
| **Requests + RFI/NOD/EOT + VOQ/VO** (`ProjectRequests.razor`, `ProjectRequestDetail.razor`, `ProjectVoqDetail.razor`, Requests feature) | **Client** | Becomes the Client folder's flagship register ("Requests"). General request = the default client container; promotion ladder untouched. VOs/VOQs remain inside the register (one lifecycle, as the current code comment says) — no separate nav entry. Reply/draft handlers gain the belt-and-braces Client-thread assertion. |
| **Valuation report snapshots** (`ValuationReportSnapshotCapture`, `TakeValuationReportSnapshot`, list/get/delete) | **Client** | Gets its own page for the first time (today snapshots are reachable only through the valuation flow): a read-only register of frozen client-facing statements, `/projects/{id}/valuation-snapshots`. The Valuation Report working tab itself stays in Financials — the working document is internal; the snapshot is what the client sees. |
| **Bid package invites** (`ProjectBidPackageInvites.razor` + detail, Procurement feature) | **Subcontractor** | Nav home moves from Operations to the Subcontractor folder. Invite drafts stamp `JPMS/Subcontractor` alongside `JPMS/BPI-####` (worker parity via the linked `TriageCategories` include). Response mail inherits both via the sweep. |
| **Work orders / WO allocation** (`ProjectWorkOrders.razor`, `ProjectWorkOrderAllocation.razor`, Commercial feature) | **Subcontractor** | Nav move only; award flow, Xero line linking and `WorkOrderCostApportionment` untouched. WO Allocation keeps its finance-flavoured role gate (see §6 gating). |
| **Todos** (`ProjectTodos.razor`, flat `/todos`, Todos feature) | **Internal** | The Internal folder's "Todo" entry. Project todos + company-wide (`ProjectId == ""`) in one place — propose the entry routes to the project todos tab with a "Company-wide" toggle that surfaces the general list (the retired flat `/todos` page's query already exists: `ListAllTodoItems`). Todo links stay pathway-neutral (§2.1). |
| **Labour / Workers / My Day** (Labour feature) | **Internal** | Nav move only (Labour from Operations; Workers from Company). No mail interaction; no wall exposure (workers see hours, never £). |
| **Drawings** (Drawings feature) | Project | Stays project-plumbing. Note for later: architect-issued drawings arrive by email through triage — those threads are Client-pathway (architect ≈ client side per the correspondence model), but the drawing register itself is pathway-agnostic. |
| **Programme + LADs** (`ProjectProgramme.razor`, Scheduling/Lad records) | Project (mail → Client) | Tab stays in Project folder; `SCH-`/`LAD-` mail maps Client per the buckets plan ⚠ defaults (confirm at slice 1 as planned). |
| **Progress** (Progress feature) | Project | No change. |
| **Communications** (`ListProjectCommunicationsHandler`) | Project | Gains the pathway segmented control on top of the existing type filter — server-side `Bucket` param intersecting one exact category clause into the existing adaptive read (≤10-tag OR filter / marker-scan fallback both compose with one extra AND). |
| **Project settings** | Project | No change. |
| **Financials / Cashflow / Valuation Report / CVR** (Commercial, Cashflow features) | Financials | Nav regroup only. Valuation *invoices* keep their own audit trail. |
| **Financial Summary / Cash Summary / Xero / Cost codes & Rates** (`FinanceOverview`, `CashSummary`, Xero feature, cost codes + rates) | Financials | Company-scoped entries join the Financials folder (§6). `WorkspaceSections` in-page sub-tabs (Xero: Allocation/Transactions; Setup: Cost codes/Rates) survive unchanged. Note: cost-centre *mail* splits by side — valuation-side → Client, subcontract-side → Subcontractor (the buckets plan's ⚠ on `CC-` becomes: the triager's pathway choice decides, rather than one global default; the mapping table's CC row changes from a fixed bucket to "ask the pathway selector"). |
| **Directory (subs) / Clients / Architects / staff** (`Subcontractors.razor`, `Clients.razor`, `Architects.razor`, Directory feature) | Directory | Unified: one Directory page with filter chips — Clients, Architects, Subcontractors, Internal staff — replacing three nav entries and folding in the RBAC user list as the "Internal staff" filter. The underlying entities already converged (unified company directory with `DirectoryCategory`; Clients/Architects as first-class party entities; `DirectoryUser` for staff) — this is a presentation-layer merge, not an entity migration. Per-category detail panes keep their existing components (contacts/correspondence profile on Clients & Architects, compliance docs on Subcontractors). |
| **Agents / AI** (`RecommendTriageActionHandler`, agent system) | Cross-cutting | Recommendation output gains a `SuggestedPathway`; recommended-vs-chosen lands in the audit trail — which incidentally builds the feedback corpus `triage-recommend-action-prompt.md` §7 wanted. |
| **Retired routes** (`/agents`, `/rfis`, `/nurture`, `/sales-analytics`, `/estimating-queue`, `/my-day`) | — | Unchanged; still reachable by URL, still out of nav. `/my-day` remains the site-operative home. |

---

## 6. The new side panel

### 6.1 Structure

Six **collapsible folders** (confirmed), one list, project picker retained at top. Slugs and
routes all survive unchanged (the `ProjectSections` convention: labels move, slugs don't).

```
Home
[Project picker ▾]

▸ Client                     (project-scoped)
    Requests                 → /projects/{p}/requests
    Valuation Report Snapshots → /projects/{p}/valuation-snapshots   (new page)
▸ Subcontractor              (project-scoped)
    Bid Package Invites      → /projects/{p}/bid-package-invites
    Work Orders              → /projects/{p}/work-orders
    WO Allocation            → /projects/{p}/work-order-allocation
▸ Internal                   (mixed scope)
    Triage                   → /requests/triage                      (company)
    Todo                     → /projects/{p}/todos                   (+ company-wide toggle)
    Labour                   → /projects/{p}/labour
    Workers                  → /labour/workers                       (company)
    Audit Trail              → /audit                                (new page, company)
▸ Project                    (project-scoped)
    Drawings                 → /projects/{p}/drawings
    Programme                → /projects/{p}/programme
    Progress                 → /projects/{p}/progress
    Communications           → /projects/{p}/communications
    Project Settings         → /projects/{p}/settings
▸ Financials                 (mixed scope)
    Financials               → /projects/{p}/financials
    Valuation Report         → /projects/{p}/valuation
    Cashflow                 → /projects/{p}/cashflow
    Financial Summary        → /finance                              (company)
    Cash Summary             → /finance/cash-summary                 (company)
    Xero                     → /finance/allocation (+ /finance/xero) (company)
    Cost Codes & Rates       → /cost-codes (+ /rate-library)         (company)
▸ Directory                  (company)
    Directory                → /directory  with filter chips:
                               Clients · Architects · Subcontractors · Internal staff

Signed in as … / Sign out
```

Additions relative to the brief's list: **Audit Trail** under Internal (the brief requires the
register; Internal is its natural home), and **Financial Summary** under Financials (it exists
today and the brief's Financials list already had a "Financials" company-level slot). The
brief's Directory sub-items are realised as filter chips on one page rather than four nav
entries.

### 6.2 Mechanics (what changes in code)

- `ProjectSections.cs` → generalises to a **folder catalog**: six `ProjectSectionInfo`-style
  groups whose tabs may be project-scoped (`/projects/{p}/…`) or absolute. The existing
  `NavigationItem` record already handles absolute hrefs, `{project}` templates and match
  prefixes — the change is that `DesktopNavigation.CompanyEntries` largely dissolves into the
  folders. `RoleHome`'s destination cards read the same catalog, so the landing page reorganises
  itself for free.
- `SideNav.razor` gains per-header collapse: chevron on each folder header, collapsed state per
  folder persisted per user (localStorage via JS interop is fine in the Blazor app; default:
  all expanded, or expand-the-folder-owning-the-current-route). The existing whole-rail
  collapse (icon rail) is orthogonal and stays; collapsed rail shows one icon per folder as it
  does per block today.
- **No-project-selected state:** project-scoped entries need a selected project. Today the
  workspace blocks only render under the picker; the unified list keeps that behaviour — with
  no project selected, project-scoped rows render disabled (or the folder shows only its
  company rows), and picking a project activates them. `ProjectDetail`'s redirect target
  (`ProjectSections.All[0].FirstTab`) follows the new first folder → first tab = Client →
  Requests, which is a sensible new default landing.
- **Role gating per row, folder visible if any row is:** existing gates carry over exactly
  (Triage: PM/FD; Cash Summary: MD/FD; Workers: MD/FD/PM; Directory: MD — widen to MD/PM if
  the merged page should keep Clients/Architects reachable by PMs, since today PMs see those
  two pages but not `/directory`; Financials rows: FinanceRoles; everything project-scoped:
  ProjectRoles). The Client folder is staff-facing nav vocabulary — `Role.Client` still sees no
  workspace at all.
- `PageHeading`, `WorkspaceSectionNav`, breadcrumbs: unchanged mechanics, retargeted at the new
  catalog.

---

## 7. Trade-offs considered

**Segregation by tag vs by storage.** The wall is category-based within one shared mailbox, not
separate mailboxes. Cheap, reversible, Outlook-visible, and consistent with the entire existing
architecture (mailbox = source of truth, no copies) — but it protects portal surfaces, not
Outlook itself, and it depends on the tag invariants holding (hence guards at the single link
choke point + the conflict report + `WallRejected` events). Separate mailboxes per pathway
would give ACL-grade separation at the cost of killing the single-queue triage model,
cross-pathway threads becoming forwards, and a much harder migration. Not recommended now; the
tag model leaves that door open later.

**Pathway-first triage adds a click.** Today a triager can jump straight to "create todo". The
router model front-loads a classification on every email. Mitigations: the AI recommendation
pre-selects the pathway (sender classification already exists), and the pathway auto-derives
anyway the moment an action is chosen — the selector is a filter, not a mandatory form field, so
a power user who goes straight to an action loses nothing (the action implies the pathway).
The gain — customised action pathways, cleaner action panel, and the audit semantics of an
explicit routing decision — outweighs the friction.

**Hard wall vs user freedom.** Blocking Client↔non-Client cross-filing means the genuine
mixed thread (a client email that needs subcontractor pricing) must be handled by forward/new
thread or by pathway-neutral links (todos). That is deliberate: the cost is an occasional extra
forward; the benefit is that no filtering bug can ever show a client a subcontractor email,
because the data is never co-tagged in the first place. The Sub↔Internal override exists
because that boundary is organisational, and forcing forwards there would be pure friction.

**One audit table vs per-feature logs.** A single `AuditEvents` table denormalises pathway +
record reference and stays schema-stable as features grow; per-feature tables (the
ValuationInvoice pattern) are richer but need a union view for "all audit events" and a new
table per feature. The split chosen: cross-cutting communication/triage events → the one table;
deep domain lifecycles (valuation invoices) keep their specialised logs. The audit table is
append-only and written in the same transaction as the action, so it can't drift from reality;
mailbox-side tag state can still drift from *it* (tags edited directly in Outlook bypass the
portal) — the conflict report is the reconciliation for that.

**Six folders is more nav than three blocks + a flat list.** Collapsible headers (confirmed)
are the mitigation, and the grouping matches how the business now thinks (who the
correspondence is with, then the job, then the money, then the people). The risk of "Triage
under Internal" reading as "internal mail only" is handled by copy — the queue's own header
should say what it is (the router for everything) — and costs nothing structurally.

**Mixed scope inside folders.** Internal and Financials folders mix project rows and company
rows. Alternative was keeping two scopes (workspace vs company), which preserves purity but
contradicts the confirmed design. The disabled-state rule (§6.2) keeps the mixed folders honest
when no project is selected.

**`CC-` mail loses its single global default.** Making the triager's pathway choice decide
valuation-side vs subcontract-side cost-centre mail is more accurate than one global default,
at the cost of a per-email decision. The pathway selector makes that decision explicit and
audited — and the buckets plan's ⚠ flag said exactly this needed a human call.

---

## 8. Build slices (extending the buckets plan's slices; each verified against the live mailbox)

1. **Tag plumbing + mapping** — unchanged from buckets plan slice 1 (`TriageCategories` bucket
   constants, `BucketFor`, `IsBucketTag`, queue-membership exclusions), plus: `BucketFor`
   returns "ask" for `CostCentre` (no auto-stamp; pathway comes from the triage choice), and
   the wall/lane distinction lands in the conflict rules.
2. **Audit trail foundation** — `AuditEventEntity` + migration, `AuditTrail.Append`, writers in
   the three discard/restore/remove handlers and `LinkMessageToRecordHandler` (they exist and
   are narrow); `ListAuditEvents`; minimal `/audit` register page. Shipping this second means
   every later slice's behaviour is observable from day one.
3. **Stamp on link/create + wall & lane guards** — buckets plan slice 2 with the two-tier rule
   (§2.3), `AllowCrossPathway` flag, `WallRejected`/`CrossPathwayOverride` events, worker
   parity for outbound drafts (`DraftCreated` events + pathway category on invites, request
   docs, replies, WO emails).
4. **Backfill** — buckets plan slice 3 unchanged (dry-run first; conversations mapping to both
   sides of the wall go to the conflict report, never guessed), plus `BackfillStamped` events
   and mailbox master-category colours for the three pathway tags.
5. **Triage UI: the router** — pathway selector + filtered action panel + consequence line +
   AI `SuggestedPathway`; bucket chips/badges and guarded "add another tag" on the Tagged view.
6. **Side panel folders** — catalog generalisation, collapsible headers, no-project disabled
   state, `RoleHome` cards follow, `ProjectDetail` redirect retarget. Pure front-end; slugs
   unchanged.
7. **New pages + regrouped surfaces** — Valuation Report Snapshots register page; Todo
   company-wide toggle; Communications pathway segmented control (server `Bucket` param);
   unified Directory page with the four filter chips (fold `/clients` + `/architects` +
   `/directory` + staff list; old routes redirect).
8. **Copy pass + role-gate review** — "Client Requests" naming decisions (the buckets plan
   proposed renaming the register; with a whole Client folder in the nav, the shorter "Requests"
   inside it may now be enough — decide with the folder UI in front of us), Directory gate
   widening (MD/PM), triage header copy.

Each slice is independently shippable; 1–4 are invisible-to-harmful-never (tags + log only),
5–7 are the visible change, 8 is polish.

---

## 9. Open questions

1. **Directory gate:** unified Directory folds in Clients/Architects, which PMs can see today,
   but `/directory` is MD-only. Widen the merged page to MD/PM (with subcontractor compliance
   detail possibly still MD-only), or keep MD-only and lose PM access to client/architect
   contact books?
2. **Valuation-snapshots page scope:** read-only register (list + open PDF/detail), or should
   capture/supersede actions live there too (they currently sit in the invoice submit flow)?
3. **Todo company-wide toggle vs restored flat page:** one toggle on the project tab, or bring
   the retired `/todos` browser back as the Internal folder's Todo entry when no project is
   selected?
4. **`ThreadSwept` audit events:** off by default per §4.2 — confirm, or is sweep-level
   traceability wanted for the wall's sake (a swept-in reply inherits the pathway silently)?
5. **Buckets plan ⚠ defaults:** SCH-→Client and LAD-→Client still stand; CC- moves from a
   global default to per-email choice (§5). Confirm all three at slice 1 as originally planned.
