# Correspondence Profile — To / CC / BCC Extension Plan

**Status:** Implemented · July 2026 (steps 1–4 of the rollout below; step 5 — dropping `ReceivesRequests` and the legacy party email fields — deliberately deferred one release)
**Goal:** Extend the single-address "Corresponds with" party link into a project-level correspondence profile — one To correspondent plus any number of CC and BCC recipients — linked to the client/architect's own communication preferences, so an RFI can be issued to a person while copying others (including internal Jewel staff).

---

## 1. Current state

Recipient resolution today produces exactly **one To address** and exists in three places that must stay in agreement:

| Path | File | Behaviour |
|---|---|---|
| Auto-send on promotion | `api/Features/Requests/Commands/PromoteRequestToRfiHandler.cs` (`ResolveRecipientEmailAsync`) | Request party → project party → legacy Architect contact flagged `ReceivesRequests` |
| Outlook draft | `api/Features/Requests/Commands/PrepareRequestEmailDraftHandler.cs` (`ResolveRecipientsAsync`) | Ad-hoc override → request party → project party → flagged project contacts |
| Worker send | `worker/MailboxIntake/Actions/MailboxActionWorker.cs` (`SendRequestDocumentAsync`) | Override → `model.Recipients` (flagged project contacts, collated by `RequestDocumentBuilder`) |

The party link (`PartyKind` + `PartyId` on the request, falling back to the project) resolves to a single email: `ClientEntity.PrimaryContactEmail` or `ArchitectEntity.ContactEmail`.

A multi-recipient mechanism half-exists: `ProjectContactEntity` with a boolean `ReceivesRequests`. It is only a fallback, is all-To, and **has no jpms UI** — the `api/Features/Projects/Contacts/*` endpoints are currently uncalled by the front end.

Graph plumbing: the API's `MailboxDraftMessage` already supports `Bcc` (needs `Cc`); the worker's `GraphOutboundMessage` supports neither.

## 2. Target model

Two layers, so party-level preferences flow into every project but each project can fine-tune:

**Party communication preferences (new)** — each Client/Architect carries a contact book: the people at that organisation and each person's default routing (To / Cc / Bcc / None). The primary To person supersedes the single `PrimaryContactEmail` / `ContactEmail` field.

**Project correspondence profile (evolved `ProjectContactEntity`)** — per project:
- *Linked rows* reference a party contact and may override its routing for this project. Name/email read through from the party contact, so an email change at the architect record propagates to every project.
- *Ad-hoc rows* carry their own name/email with no party link — this is how internal Jewel staff (or any third party) get CC'd/BCC'd.

Effective recipient set for a project = party contacts at their default routing, overlaid by the project's linked overrides, plus the project's ad-hoc rows.

## 3. Data model changes

New shared enum (in `contracts/Models`):

```csharp
public enum CorrespondenceRouting { None = 0, To = 1, Cc = 2, Bcc = 3 }
```

New entity `PartyContactEntity` (in `api/Data/Entities`, linked into the worker like the other entities):

```csharp
string PartyContactId   // PK
int    PartyKind        // Client / Architect
string PartyId
string Name
string Email
string? JobTitle
int    DefaultRouting   // CorrespondenceRouting
bool   IsPrimary        // exactly one To-primary per party
DateTimeOffset CreatedAt
```

Evolve `ProjectContactEntity`:
- Add `int Routing` (CorrespondenceRouting).
- Add `string? PartyContactId` — set = linked row (Name/Email resolved from the party contact at read time); null = ad-hoc row.
- Deprecate `bool ReceivesRequests` (keep the column one release for rollback; stop reading it).

**Migration + backfill** (one EF migration):
1. Create `PartyContacts`; seed one row per Client/Architect from `PrimaryContactEmail` / `ContactEmail` with `DefaultRouting = To`, `IsPrimary = true`. Keep the legacy columns in sync on write until the UI moves over, then drop.
2. Add `Routing` and `PartyContactId` to `ProjectContacts`; backfill `Routing = To` where `ReceivesRequests = 1`, else `None`.

## 4. Shared recipient resolution

Replace the three divergent resolvers with one `RequestRecipientResolver` (api project, file-linked into the worker like the entities) returning:

```csharp
public sealed record RecipientSet(
    IReadOnlyList<Recipient> To,
    IReadOnlyList<Recipient> Cc,
    IReadOnlyList<Recipient> Bcc);
```

Resolution order:
1. **Ad-hoc override** (`RecipientOverride`, unchanged semantics): To = the one address, **no Cc/Bcc** — a resend to one person stays exactly that.
2. **To** — the request's party link wins (its party's `IsPrimary` contact, falling back to the legacy email field), then the project's party, then project profile rows with `Routing = To`.
3. **Cc / Bcc** — always the project's effective profile: the To-party's contacts whose effective routing is Cc/Bcc (default routing overlaid by any project-level override), plus ad-hoc project rows. The resolved To address is de-duplicated out of Cc/Bcc.

Callers: `PromoteRequestToRfiHandler`, `PrepareRequestEmailDraftHandler`, `MailboxActionWorker`, and `RequestDocumentBuilder`. The queue message (`MailboxActionMessage`) needs no change — the worker already re-resolves from SQL.

## 5. Graph plumbing

- API `MailboxDraftMessage` (`api/Features/MailboxIntake/Graph/MailboxGraphClient.cs`): add optional `Cc`; emit `ccRecipients` in `BuildMessagePayload` (Bcc already handled).
- Worker `GraphOutboundMessage` (`worker/MailboxIntake/Graph/IGraphMailClient.cs`): add optional `Cc` and `Bcc`; emit `ccRecipients` / `bccRecipients` in `GraphMailClient.SendMailAsync`.

## 6. Document and audit-trail rules

- `RequestDocumentModel.Recipients` gains a routing marker but carries **only To and Cc — never Bcc**. Keeping Bcc out of the document model makes a Bcc leak onto the PDF structurally impossible; the resolver supplies Bcc at send/draft time only.
- The PDF's issued-to block shows To ("Issued to") and Cc ("Copied to").
- The worker's Shared activity message ("RFI-0007 document issued to …") is client-facing: it lists **To + Cc only**. Bcc is logged internally (count only) via `ILogger`.
- `RequestEmailDraft` (contract) gains `Cc`/`Bcc` lists so the UI can confirm what the draft carries (the person drafting is internal, so showing Bcc there is correct).

## 7. API / contracts

- New CQRS trio for party contacts: `ListPartyContacts`, `UpsertPartyContact`, `RemovePartyContact` (mirror the existing project-contacts feature; same authorisation shape).
- Extend `UpsertProjectContact` / `ProjectContact` with `Routing` and `PartyContactId`; `ListProjectContacts` returns the *effective* profile (linked rows resolved, overrides applied).
- New query `ResolveRequestRecipients(RequestId)` returning `RecipientSet` — powers a recipients preview on the request page so nobody is surprised by what Promote/Draft will send.
- `PrepareRequestEmailDraft` unchanged in shape; doc comment updated (override = To only).

## 8. jpms UI

Follow the store convention (CLAUDE.md): new stores fetch once per key; pages call `Refresh(...)` from `OnInitializedAsync`.

- **Clients.razor / Architects.razor** — a Contacts section per party: add/edit people, set default routing, mark the primary correspondent. Replaces the single contact-email field as the source of truth.
- **ProjectDetail** — extend the "Corresponds with" control (`Components/ProjectDetailsEditor.razor`) into a Correspondence panel: the party select as today, beneath it the inherited party contacts with their effective routing (toggle To/Cc/Bcc/None per project), and an "Add recipient" row for ad-hoc entries such as internal Jewel staff.
- **ProjectRequestDetail** — the "Issued to" panel keeps its party select and adds a read-only resolved preview (To / Cc / Bcc via `ResolveRequestRecipients`) so the send is predictable before Promote to RFI or Draft email.

## 9. Rollout order

1. Migration + entities + backfill (behaviour unchanged: resolver not yet switched).
2. `RequestRecipientResolver` + swap the three call sites; Graph Cc/Bcc plumbing; document/audit rules. Old data yields identical output (backfilled `To` rows ≡ old `ReceivesRequests` fallback), so this is safe to ship without a flag.
3. Party-contacts endpoints + Clients/Architects UI.
4. Project Correspondence panel + request-page preview.
5. Drop `ReceivesRequests` and legacy party email fields once the UI writes exclusively through contacts.

## 10. Edge cases

- **No To resolvable** — same failure text as today, extended to mention routing ("…or set a contact's routing to To").
- **Duplicate addresses** — resolver de-duplicates across To > Cc > Bcc (highest visibility wins).
- **Party changed on a project** — linked rows belonging to the old party are dropped from the effective set automatically (they resolve through the party link); ad-hoc rows survive.
- **Architect on behalf of client** — unchanged: `OnBehalfOfClientId` remains display metadata; routing follows the corresponding party. A common fine-tune will be CC'ing the client's primary contact on a project corresponded via the architect — that's one project-level linked row pointing at the client's party contact.
- **Bid package invites** (`SendBidPackageInviteHandler`) already build `MailboxDraftMessage` directly and are out of scope, but gain Cc support for free.
