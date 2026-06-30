# Linkable Record Container — Implementation Plan

Status: **Draft for review** · Date: 2026‑06‑30 · Relates to: `docs/Bid-Package-Invite-and-Agent-System-Spec.md` (Part B, Phase 1)

## 1. What we're building

Today the triage "Link to existing" panel links an email to one kind of thing: a **Request**. We are generalising it so an email (or a set of emails) can be linked to a **record** of any type — currently a Request or a Bid Package Invite, with more record types to come (e.g. an Invoice or Credit Note in a finance module).

The mental model the user described: a record is a domain‑specific SQL entity. Each record is a small **container** that houses (a) the record's own document/fields, (b) the set of emails linked to it, and (c) the agent(s) that read those emails to keep the record updated. The link layer between an email and a record is the only thing shared across record types — everything else is type‑specific.

The triage panel therefore needs to become: **pick a category (record type) → pick a project → pick a record of that type → link.** When any record is later opened, all its linked emails appear (this already works for Requests and generalises for free once the read layer is type‑agnostic).

Crucially, **nothing about the link mechanism itself needs to change** — it is already the right design. An email is "linked" by stamping an Outlook category tag `JPMS/<reference>` on it; the tag *is* the association; a record reads its mail live by querying that tag. We are not adding a join table. We are lifting the existing tag‑based link off the `Request` type and onto a record‑type‑agnostic seam.

## 2. Current state — where Request is hardwired

The link/read path is clean but bound to `Request` at five points:

| Layer | File | Binding to Request |
|---|---|---|
| Generic link command | `contracts/Requests/AssignMessageToRequest.cs` | Command carries `RequestId`; handler tags `JPMS/<request.TagReference>`. |
| Command handler | `api/Features/Requests/Commands/MailboxRequestCommandHandlers.cs` | `AssignMessageToRequestHandler` loads a `RequestEntity`, calls `graph.AssignAsync(... TriageCategories.ForRequest(request.TagReference))`. |
| Frontend service | `jpms/Services/IIntakeQueue.cs` + `HttpIntakeQueue.cs` | `AssignMessageAsync(messageId, imid, requestId)` → `AssignMessageToRequest`. |
| Triage UI | `jpms/Pages/TriageQueue.razor` | Link panel lists `RequestRegister.ForProject(projectId)`; `DuplicateCandidates()` scores by subject overlap; `DoLink()`/`DoAddTagLink()` send a request id. |
| Live email read | `api/Features/Requests/RequestEmailReader.cs` | `ForRequestAsync(requestId)` resolves `request.TagReference` then `graph.ListByTagAsync($"JPMS/{ref}")`. |

The two pieces that are **already type‑agnostic and stay as‑is**:

- `TriageCategories` / `MailboxGraphClient` (`api/Features/MailboxIntake/Graph/MailboxGraphClient.cs`) — tag write/read/verify. The tag is just a string; it does not know or care what record type it points at.
- `RecordType` enum (`contracts/Models/Agent.cs`: `Request`, `BidPackageInvite`) and the agent‑by‑type registry from Phase 0 — the record‑type discriminator already exists.

So the refactor is narrow: introduce a record abstraction over the five bound points, and add a Bid Package Invite implementation of it.

## 3. Target architecture — the linkable‑record seam

### 3.1 Contract: a record envelope

Add a small projection that any record type can produce, used by the triage list and match UI (no document fields — just identity + what's needed to pick and de‑dupe):

```csharp
// contracts/Models/LinkableRecord.cs
public sealed record LinkableRecord(
    RecordType Type,        // Request | BidPackageInvite | …
    string     RecordId,    // the SQL entity's id
    string     ProjectId,
    string     Reference,   // human ref shown in the list, e.g. "RFI-001" / "BPI-0001"
    string     TagReference,// canonical tag stem (usually == Reference)
    string     Title,
    string?    StatusLabel = null); // small badge, type-specific
```

### 3.2 Backend: a provider per record type

Each record type registers a provider. This is the single seam the generic command/query/reader talk to:

```csharp
// api/Features/RecordLinks/ILinkableRecordProvider.cs
public interface ILinkableRecordProvider
{
    RecordType Type { get; }
    Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct);
    Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct); // resolves TagReference for link + read
}
```

- `RequestLinkProvider` wraps `context.Requests` (existing `ToModel`/`TagReference`).
- `BidPackageInviteLinkProvider` wraps the BPI entity (section 5).
- A `RecordProviderRegistry` resolves `ILinkableRecordProvider` by `RecordType` (mirrors the existing `AgentRegistry.ForRecordType` pattern).

### 3.3 Backend: one generic link command + query + reader

**Generic link command** (replaces direct use of `AssignMessageToRequest`; keep the old command as a thin adapter for back‑compat — see 4.2):

```csharp
// contracts/RecordLinks/LinkMessageToRecord.cs
public sealed record LinkMessageToRecord(
    string MessageId, RecordType Type, string RecordId, string? InternetMessageId = null)
    : ICommand<Acknowledgement>;
```

Handler: registry → provider → `FindAsync(recordId)` → `graph.AssignAsync(msg, imid, TriageCategories.ForRequest(record.TagReference))`. Note `ForRequest` is just "stem a reference into a `JPMS/` tag" — rename to `TriageCategories.ForRecord(reference)` to drop the Request‑specific name (keep `ForRequest` as an alias for one release).

**Generic catalogue query** for the triage list:

```csharp
// contracts/RecordLinks/ListLinkableRecords.cs
public sealed record ListLinkableRecords(string ProjectId, RecordType Type)
    : IQuery<IReadOnlyList<LinkableRecord>>;
```

**Generic email reader**: generalise `RequestEmailReader` → `RecordEmailReader.ForRecordAsync(RecordType type, string recordId, CancellationToken ct)` that resolves the tag via the provider then reads by tag. Keep a `ForRequestAsync` shim so existing callers (conversation view, agent context, document builder) compile, then migrate them.

### 3.4 Frontend: service + read model

- Extend `IIntakeQueue` with `LinkMessageToRecordAsync(messageId, imid, RecordType, recordId)` (keep `AssignMessageAsync` as a Request‑typed shim).
- Add a client read model `IRecordCatalog.ForProject(projectId, RecordType)` → `LinkableRecord[]` (mirrors `HttpRequestRegister`, backed by `ListLinkableRecords`).

## 4. Frontend changes — `TriageQueue.razor`

### 4.1 Link panel becomes category‑first

Current Link panel (lines ~245–305) goes Project → Requests. New order:

1. **Category** select (record type) — `Request`, `Bid Package Invite`. Drives everything below. New state `linkRecordType` replaces the implicit "always Request".
2. **Project** select (unchanged control, now feeds the catalogue with the chosen type).
3. **Record list** — replace `ProjectRequests()` with `RecordCatalog.ForProject(linkProjectId, linkRecordType)`. The "possible matches" / "other records" split and the subject‑overlap scoring (`DuplicateCandidates`/`Overlap`/`Tokenise`) move to operate on `LinkableRecord` (they only read `Reference` + `Title`, so they generalise unchanged).
4. **Per‑category detail row** — a small `RenderFragment` keyed by `linkRecordType` controls which fields show on each record button (e.g. Request shows `Reference + Title`; BPI shows `Reference + Trade + Title`). This is the "show the detail according to the category selected" requirement.
5. **Link button** → `Intake.LinkMessageToRecordAsync(selected.Id, imid, linkRecordType, linkRecordId)`.

State changes: replace `linkRequestId` with `linkRecordId`; add `linkRecordType`; reset both when category or project changes.

### 4.2 Back‑compat / migration

- `AssignMessageToRequest` command + `AssignMessageAsync` stay, implemented as adapters that call the generic path with `RecordType.Request`. This keeps the Tagged‑tab "add another tag" flow (`DoAddTagLink`) and any other callers working during the migration, and lets us land the refactor without a big‑bang UI rewrite.
- The Tagged tab's "Add tag (link to request)" control (lines ~600–625) generalises the same way: category → project → record.

### 4.3 "Create new" path (secondary, can be a follow‑up)

The "Create new request" tab is the sibling of "Link to existing": it creates a record from the email and links in one step. Generalising it (category → per‑type create form) is the same registry idea but with a create‑from‑message handler per type. **Recommendation:** ship "Link to existing" generalisation first; generalise "Create new" in the BPI record phase, since a BPI create form needs the BPI fields anyway.

## 5. Bid Package Invite as a linkable record (prerequisite)

A record can only appear as a category once it exists as a listable entity with a tag stem. The spec's open decision **D4** (extend existing `BidPackage` vs. add BPI) — recommendation: **add a thin `BidPackageInviteEntity`** now with just enough to be linkable, and let the richer invite data (recipients, responses, blob files) land in later BPI phases. Minimum fields:

```
BidPackageInviteEntity:
  InviteId (PK), ProjectId, Number (seq), Reference (BPI-0001), Title, Trade,
  Status (Draft|…), CreatedAt, CreatedByEmail
  TagReference => Reference (== "BPI-####")
```

Then:
- `ListBidPackageInvitesForProject` query + handler (mirror `ListBidPackagesForProjectHandler`).
- `BidPackageInviteLinkProvider : ILinkableRecordProvider` (Type = `BidPackageInvite`).
- Register both in `ProcurementFeatureRegistration`.

The Bid Packages agent (already mapped to `RecordType.BidPackageInvite` from Phase 0) reads the invite's linked emails through the generic `RecordEmailReader` — this is the "an agent works the linked emails to update the record" loop, now type‑agnostic.

## 6. Tag namespacing — the one correctness risk

Tags are a flat string space (`JPMS/<ref>`). Requests use `RFI-001`, BPIs use `BPI-0001`, so they don't collide **as long as every record type's reference prefix is unique**. Make this explicit rather than incidental:

- Give each provider a reference prefix it owns (`RFI/RFA/…`, `BPI`, future `INV`).
- Recommendation: keep the tag exactly `JPMS/<reference>` (no type segment) so existing `JPMS/RFI-001` tags on live mail keep resolving — **no re‑tagging of existing mail**. The type is recoverable from the prefix and is also carried in the link command, so a type segment would be redundant.
- Add a guard test asserting reference prefixes are disjoint across providers.

## 7. Open decisions to confirm

1. **D2 (resolved by you):** record types are independent first‑class SQL entities sharing only the link layer — this plan reflects that (no `RequestType.BidPackageInvite`, no shared table).
2. **Match scoring location:** keep the subject‑overlap de‑dupe client‑side over `LinkableRecord` (simplest, reuses existing code) vs. move per‑provider server‑side. *Recommend client‑side for now.*
3. **Tag stem format:** flat `JPMS/<ref>` with prefix‑owned references (recommended, no migration) vs. typed `JPMS/<type>/<ref>` (cleaner namespacing, requires re‑tagging existing mail). *Recommend flat.*
4. **BPI record shape:** thin invite entity now (recommended) vs. extend `BidPackageEntity`. Confirm before I create the entity/migration.
5. **"Create new" generalisation:** defer to the BPI record phase (recommended) vs. do it in this pass.

## 8. Suggested sequence (small, reviewable PRs)

1. **Seam, no behaviour change.** Add `LinkableRecord`, `ILinkableRecordProvider`, `RecordProviderRegistry`, `RequestLinkProvider`. Add generic `LinkMessageToRecord` + `ListLinkableRecords` + handlers. Re‑implement `AssignMessageToRequest`/`AssignMessageAsync` as adapters. Generalise `RequestEmailReader` → `RecordEmailReader` with a `ForRequestAsync` shim. Rename `ForRequest` → `ForRecord` (alias kept). *Build is green, UI identical.*
2. **Triage UI category‑first.** Add the Category selector, `IRecordCatalog`, per‑type detail fragment; route Link + Tagged‑tab linking through the generic path. *Request is still the only category — proves the generalised UI with one type.*
3. **BPI linkable record.** Add `BidPackageInviteEntity` + migration, list query, `BidPackageInviteLinkProvider`, registration. BPI now appears as the second category. *Delivers the two‑category panel.*
4. **(Follow‑up) Generalise "Create new"** + BPI create form; wire the Bid Packages agent over `RecordEmailReader`.

## 9. Verification

- **Build:** `dotnet build` (the Phase 0 note flags it was static‑verified only — CI build is the gate here too).
- **Unit:** `tests/Jewel.JPMS.Tests` — add (a) provider registry resolves both types, (b) `LinkMessageToRecord` tags `JPMS/<ref>` for each type, (c) reference‑prefix disjointness guard, (d) `RecordEmailReader.ForRecordAsync` reads back a tagged message for a BPI. Keep an `AssignMessageToRequest`‑adapter test to prove back‑compat.
- **Manual:** link an email to a Request and to a BPI; open each record and confirm the email appears; remove the tag and confirm it drops from the record.
- **Regression to watch:** the marker/last‑tag‑removal logic in `MailboxGraphClient.RemoveTagAsync` is unchanged but now exercised by two reference families — include a BPI tag in the remove‑last‑tag test.

## 10. Out of scope (named so they're not assumed)

Rich BPI invite data (recipients from the directory, response intake, Excel/blob, draft email, award→PO→cost‑centre) — those are the later BPI phases in the parent spec. This plan delivers only the **container + link layer** generalised to two record types, which is the foundation they sit on.
