# JPMS Agent Architecture

> Status: **design, not built.** This document is for the team to attack before code exists.
> It supersedes `docs/03-workflows/10-request-agents.md` §6 (Future work).
>
> Terminology follows `CLAUDE.md` throughout: **programme**, **valuation invoice**, **variation**
> (one document, one number, read as `V72`). Do not drift.

---

## 0. The one-paragraph version

An agent is **not a process**. It is a *configuration*: a system-prompt fragment, a subset of tools,
and a completion policy. There is one conversation and one loop; switching "agent" swaps the
configuration, not the participant. The loop runs in **two runtimes** — the Blazor client drives
interactive chat (so every tool call carries the user's own session cookie and inherits their RBAC
for free), and the worker drives scheduled autonomous runs. Claude never sees the database. It sees
a catalogue of typed tools that are your existing CQRS commands and queries, plus a class of
client-side tools that operate the portal UI.

The rest of this document argues for each of those choices and specifies the pieces.

---

## 1. What already exists

Before designing anything, the honest inventory. Roughly 70% of the plumbing is built; the missing
30% is the part that matters.

### Built and working

| Piece | Where | Note |
|---|---|---|
| Anthropic client | `api/Features/Ai/ClaudeClient.cs` | Real HTTP client. **Single turn only**: one system prompt, one user message, no tool use, no history, no streaming. `MaxTokens` defaults to 1024. |
| Config | `api/Features/Ai/AnthropicOptions.cs` | `Anthropic__ApiKey`, `__Model` (default `claude-sonnet-4-6`), `__ApiVersion`, `__MaxTokens`. Null-object fallback when unkeyed. |
| Two live LLM call sites | `PrepareVoqDraftHandler.cs:54`, `ExtractQuoteFromMessageHandler.cs:72` | Both single-shot JSON extraction. Both fall back to a skeleton on failure. This is the proven pattern. |
| Context assembly | `api/Features/Agents/RequestContextAssembler.cs` | Real, 95 lines, already consumed by `PrepareVoqDraftHandler`. Builds a plain-text header + merged conversation (SQL messages + live Graph emails). **Attachments are metadata-only.** |
| Agent persistence | `api/Data/Entities/AgentEntities.cs` | `RequestAgentEntity`, `AgentChatMessageEntity`, `AgentProposalEntity`. Chat body capped at 4000 chars. |
| Agent CQRS surface | `api/Features/Agents/{Commands,Queries}` | 4 commands, 4 queries, each with the full Endpoint/Handler/Authorisation/Validation convention. |
| Human-in-the-loop UI | `jpms/Components/AgentWorkspace.razor` | 271 lines, complete: agent list, chat thread, run-analysis, Accept/Reject on proposals. |
| Draft-to-Outlook | `MailboxGraphClient.CreateDraftAsync:810`, `CreateReplyDraftAsync:849` | Mature. Six live callers. Auto-CCs the projects mailbox, stamps the record tag, writes a `DraftCreated` audit row with the Graph id and `WebLink`. |
| Supplier search | `BraveLocalBusinessSearch` + `WebsiteContactFinder` | Live when keyed. Google Places was deliberately dropped on licensing grounds. |
| Deterministic programme analysis | `api/Features/Agents/SchedulingAgent.cs` | 169 lines of real logic — baseline diff, `ProgrammeMovementCalculator`, JCT ICD 2024 cl. 2.19 narrative. **Not LLM-driven**, and currently unreachable. |

### Built and switched off

- `AgentProvisioning.ProvisioningEnabled = false` (`:19`). Nothing is ever attached to a request.
- Migration `20260702130000_ClearRequestAgents` deleted every agent row. `Down()` is empty.
- `<AgentWorkspace>` is commented out of `ProjectRequestDetail.razor:355`.
- `/agents` (`AgentQueue.razor`) is routable but has no nav entry and renders its empty state.
- The Claude "recommend action" triage button was **removed 2026-07-22**; its prompt survives in
  `docs/triage-recommend-action-prompt.md`.

### The three gaps that matter

1. **No tool-use loop.** `IClaudeClient.CompleteAsync(system, user, ct)` cannot express tools,
   assistant turns, or multi-step reasoning. This is the single largest piece of new work.
2. **No history reaches the agent.** `IRequestAgent.RespondAsync(context, userMessage, ct)` never
   receives prior `AgentChatMessage` rows. Even with a real model, every turn would be amnesiac.
3. **Accepting a proposal does nothing.** `DecideAgentProposalHandler:23-29` flips a status field.
   No code reads `StructuredJson` on acceptance. The human-in-the-loop model has an approval step
   and no execution step.

---

## 2. Five constraints that shape everything

These are not preferences. They are properties of the system as deployed, and every design decision
below falls out of them.

**C1 — The API cannot run long work.** It is hosted as Static Web Apps *managed* functions:
HTTP triggers only, **~45 second gateway timeout**. The code already works around it —
`api/Program.cs:64` caps SQL commands at 25s with a comment naming the gateway. A tool-use loop of
six round trips at 5–15s each does not fit, and there is no streaming escape hatch.

**C2 — Authorisation is per-request and cookie-bound.** `SignedInUserResolver.ResolveAsync` needs an
`HttpRequest` and reads the `jpms_session` cookie. There is no service-principal, API-key or
system-user path into the API. Any server-side agent has to invent one.

**C3 — The system cannot send email.** Graph permission is `Mail.ReadWrite`; `Mail.Send` is
deliberately not granted, and `IMailboxGraphClient` carries the comment *"there is deliberately NO
send method on this interface"*. Everything outbound is an Outlook **draft** a human opens and sends.

**C4 — There are no foreign keys and no transactions across features.** Every link is a loose
`string` id (`JpmsContext.cs:150-162`). There is no outbox and no event bus; cross-feature effects
happen inline in one handler, in one `SaveChangesAsync`. An agent writing rows can orphan anything.

**C5 — Emails are not stored.** The mailbox is the source of truth and the Outlook **category is the
only link**: `JPMS/{projectRef}-{reference}`, e.g. `JPMS/JBB-2026-001-RFI-049`. Removing a tag
removes the email from the record. Attachment *bytes* are fetched on demand and never copied in.

---

## 3. ADR-001 — Where the loop runs

> **Revised.** An earlier draft of this ADR put the conversation state and the tool-execution loop in
> the Blazor client. That bought RBAC inheritance cheaply, but it made the client authoritative over
> what the model was told — and in a system that drafts contractual correspondence, that is the wrong
> trade. The decision below keeps the security property and moves the authority server-side.

### Decision

**The server is authoritative. The client drives the pump, never the state.**

- **Interactive agents** (the chat panel): conversation state lives in the database. The client
  posts only *the new user message and a conversation id*, then calls a continue endpoint in a loop
  until the server says the turn is complete. Each call is exactly **one** Claude round trip plus
  the tool executions it asked for — comfortably inside the 45s gateway (C1).
- **Autonomous agents** (mailbox triage, the 9am runs): the same loop core, driven by the **worker**
  Function App on a timer, acting as an explicit system pseudo-user.

Three properties fall out, and all three matter.

**The client cannot fabricate what the model saw.** The transcript, the system prompt, the pinned
context and the tool results are assembled server-side from the database on every call. A tampered
client can send a message; it cannot inject an assistant turn, forge a tool result, or edit the
rules the model is operating under. That is what makes the persisted conversation admissible as a
record of how a notice or a variation came to be drafted.

**Tools still execute under the caller's own identity.** The endpoint resolves `SignedInUser` from
the session cookie exactly as today, and the tool dispatcher runs the same `*Authorisation.cs` class
the HTTP endpoint would have run before invoking the handler. The invariant survives intact:

> **The assistant can do exactly what the signed-in user could already do by clicking, and nothing else.**

It is now enforced in one place, on the server, rather than inherited as a side effect of where the
loop happened to run.

**Prompt injection has a much smaller blast radius.** Emails are untrusted input and the agents read
them. With server-side assembly, third-party content is delimited and labelled as data at the point
it enters the prompt, and it cannot reach the tool list or the system prompt at all.

### What still runs in the browser

Exactly one thing: **UI tools** (§5). `navigate_to`, `open_modal`, `highlight`, `set_filter`,
`ask_user` have no server-side meaning — they move the user around their own screen. The server
returns them to the client as instructions, the client executes them, and the client posts back a
result that is **advisory only**: "navigated", "user chose option B". Nothing security-relevant is
asserted by the client, so nothing is lost by trusting it.

### Why not a queue for chat

C2 (no user session outside an `HttpRequest`) is the reason. A queued turn would have to bridge the
user's identity into a background process — inventing exactly the service-principal path this system
has deliberately never had. Keeping each hop inside a request that carries the cookie avoids that
entirely. The autonomous agents accept the bridge because they genuinely have no user, and they get
a narrower capability set in exchange.

If a chat turn ever needs to outlive the browser tab — a long fan-out analysis, say — it becomes a
**background job dispatched by a tool** (§6), not a queued conversation.

### Why the worker drives autonomous runs

C2 bites here: there is no way for a timer to hold a user session. The precedent already exists —
`MailboxActionWorker` talks to `JpmsContext` and Graph directly and stamps
`ActorEmail = projects@jewelbb.co.uk` on its audit rows (`:159`). Autonomous agents follow that
shape, with a **narrower, explicitly enumerated capability set** than any human role: they may read
widely, draft email, and *propose*, but the set of things they may write without a human is a short
allow-list held in code.

### Alternatives rejected

| Option | Why not |
|---|---|
| Whole loop in one API request | C1. Six tool round trips will not fit in 45s, and there is no streaming escape hatch. Hence one Claude call per request, with the client pumping. |
| Conversation state held by the client | The original draft. Cheap and fast, but the client becomes authoritative over what the model was told — so the transcript is not evidence, and injected content could reach the system prompt. Wrong trade for contractual correspondence. |
| Loop in the worker, client polls | What the autonomous side does. For chat it needs a session-to-worker identity bridge (C2) — a service-principal path this system has deliberately never had. Not worth opening for latency alone. |
| Move the API off SWA managed functions | The clean long-term fix for C1 and worth doing eventually, but it is an infrastructure programme, not a feature. Do not couple the agent work to it. |
| Anthropic key in the browser | Never. The key stays server-side, in the API and the worker only. |

---

## 4. ADR-002 — Tools are CQRS contracts

### Decision

Every tool is backed by an existing (or new) `ICommand<T>` or `IQuery<T>`. A tool is **declared
once** as a descriptor and projected into Claude's tool schema; it is never hand-written twice.

```csharp
// contracts/Ai/ToolDescriptor.cs
public sealed record ToolDescriptor(
    string Name,                 // snake_case, what Claude sees: "list_variations_for_project"
    string Description,          // written for the model, not the developer
    Type ContractType,           // typeof(ListVariationOrdersForProject)
    ToolKind Kind,               // Read | Write | Ui
    WriteMode Mode,              // Direct | Proposal — meaningless for Read/Ui
    RoleSet VisibleTo);          // filters the catalogue before it is sent

public enum ToolKind  { Read, Write, Ui }
public enum WriteMode { Direct, Proposal }
```

The JSON input schema is **generated from `ContractType`** by reflection over the record's primary
constructor. A tool cannot drift from the command it invokes, because there is only one definition
of the shape.

The catalogue is assembled per turn and filtered three ways: by the capability pack in force (§6),
by `VisibleTo.IncludesAny(user.Roles)`, and by whether the tool's preconditions are satisfiable in
the current scope (no `approve_variation` without a variation in view). **A tool the user could not
invoke is never described to the model** — it cannot be tempted into promising something it will
then be refused.

### Read is free, write is a proposal

This is the rule that makes the whole thing safe enough to ship.

- **`Read` tools execute immediately.** Worst case is a wasted call.
- **`Write` tools default to `Proposal`.** The tool does not execute. It returns a structured
  proposal, rendered in the chat panel as a card with the exact command, the exact values, and
  Confirm / Edit / Discard. Confirm dispatches the real command through the normal path.
- **`Direct` is an opt-in allow-list**, per tool, justified in a comment. Reserved for writes that
  are cheap to reverse and carry no external consequence — adding a to-do, setting a filter,
  attaching a tag. Anything that touches money, contract status, or a third party is `Proposal`.
  Forever.

`WriteMode` is a property of the *tool*, not of the model's confidence. The model never chooses
whether something needs approving.

### Naming

Tool names are the model's vocabulary and deserve care. `list_open_variations_for_project` beats
`ListVariationOrdersForProject` — the model reads names as instructions. Keep the CQRS name in
`ContractType` and let the tool name be prose. Descriptions state **when to use it and when not to**;
the most valuable sentence in most descriptions is the negative one.

### Alternatives rejected

| Option | Why not |
|---|---|
| Raw read-only SQL + schema in context | Bypasses every `*Authorisation.cs` gate — a director's session could read anything, including other organisations' figures. Also: 111 DbSets and **no foreign keys anywhere**, so the model would invent joins that look right and silently aren't. |
| Hybrid: typed tools + sandboxed SQL | Defensible *later*, and the escape hatch if the tool surface proves too narrow. If it happens: a dedicated read-only SQL login, a curated view set with the role filter already embedded in each view, a statement timeout, and an allow-list of view names. Not v1. |
| Send the EF diagram / entity docs as context | Enormous, and `docs/05-data-model/` documents ~25 entities that have no table and status ladders that match no enum in the code. It is intent, not schema. Feeding it to a model would teach it a system we do not have. |

---

## 5. ADR-003 — The portal as a tool surface

This is the piece that makes it feel like the assistant is *operating* the portal rather than
talking about it, and it is the cheapest part of the whole design because it never touches the
server.

### Decision

A third tool kind, `Ui`, executed entirely in the Blazor client:

| Tool | Effect |
|---|---|
| `navigate_to(route, reason)` | `NavigationManager.NavigateTo`. The middle column changes under the chat. |
| `open_modal(modal_key, prefill)` | Opens a registered dialog with fields pre-populated. The user completes and submits it themselves. |
| `highlight(anchor)` | Scrolls to and pulses an element. `jpmsFocusElement` + the existing `.jpms-flash` keyframe already do this. |
| `set_filter(key, value)` | Applies a filter on the active page. |
| `ask_user(question, options)` | Renders tappable choices in the chat instead of guessing. |

`open_modal` is the important one. It is the answer to *"create new x loading a modal, with the user
able to take control at any point"* — the assistant assembles the record and presents it in the
**same dialog the user already knows**, pre-filled. They can accept it, correct one field, or close
it. Control transfer is a non-event because the destination was always the normal UI.

### The manifest is generated, never written

The model needs to know the portal's structure. That knowledge must be **derived**, or it will drift
within a fortnight.

- Routes come from `NavigationCatalog.ItemsFor(role)` — which already exists, already carries
  labels, already knows which routes are project-scoped (`{project}` templates), and is already
  role-filtered. The manifest is a projection of it.
- Modals need a new `ModalCatalog`: a registry keyed by `modal_key`, declaring the dialog component,
  its prefill contract, and the `RoleSet` that may open it. Registering a modal there is what makes
  it reachable by the assistant — an explicit opt-in, one line per dialog.
- The manifest is assembled **per user, per turn**, so it can only ever describe what that person
  can actually reach.

A route the user cannot see is not in their manifest, so the assistant cannot navigate them
somewhere they would be bounced from.

### Where it goes in the prompt

The manifest is a **pinned context block**, not a tool response — the model needs it before it can
plan. Keep it compact: route, label, one clause on what lives there. For a project manager that is
roughly 40 lines. Do not include page contents, only the map.

---

## 6. ADR-004 — Agents are configurations, not processes

### Decision

**One conversation, one loop, many capability packs.** An "agent" is:

```csharp
public sealed record CapabilityPack(
    string Key,                          // "qs", "bid-packages", "programme", "timesheet"
    string DisplayName,
    string SystemPromptFragment,         // the discipline's voice, rules and script
    IReadOnlyList<string> ToolNames,     // the subset of the catalogue it may use
    IReadOnlyList<string> PinnedContext, // glossary fragments, e.g. the variation lineage rules
    RoleSet AvailableTo,
    CompletionPolicy Completion);        // what "done" means for this discipline
```

Switching pack changes the system prompt and the tool set **within the same conversation**. History
is preserved. The model is told, in-band, that its remit has changed.

Selection happens three ways, in precedence order:

1. **Explicit** — the user says "help me with the bid package", or the Orchestrator calls
   `switch_capability(key, reason)`.
2. **Contextual** — the active route implies a default pack. `/projects/{id}/variations/{id}` opens
   in the QS pack; `/projects/{id}/programme` in the Programme pack.
3. **Fallback** — the Orchestrator pack, which can answer generally and route.

### Why not sub-agent hand-off

The obvious design is a router that delegates to child agents, each with its own conversation. It is
wrong for this product, for three reasons.

**Continuity is the feature.** The brief asks for "natural conversation [that] can flow and navigate
the portal at the same time". A hand-off resets that. Users experience it as being transferred, and
they have to re-explain. Every hand-off is a place to lose the thread.

**The packs share a context.** They are all looking at the same project, the same variation, the
same emails. Duplicating that context into a child conversation costs tokens and invites the two to
disagree about the facts.

**It is dramatically simpler to reason about.** One transcript, one audit trail, one set of tools in
force at a time. When something goes wrong you read one conversation, top to bottom. With N agents
you reconstruct a distributed trace.

### Where sub-agents *are* right

Fan-out is genuinely valuable when work is **bounded, parallel, and expensive** — "compare these 40
drawing revisions and list the changes", "read these 60 tender replies and rank them". Those are
**background jobs**, dispatched by a tool (`start_analysis(kind, scope)`), executed in the worker,
and returned as a proposal when finished. They are not conversation participants and they never
speak to the user directly.

That is the line: **conversation is single-threaded; analysis fans out.**

---

## 7. ADR-005 — Context is layered and lazy

Never assemble a large context and hope. Three layers, in ascending cost:

**Layer 1 — Ambient. Always present, ~200 tokens.**
Who the user is and their roles; today's date; the active route and its label; the active project
(`Reference`, `Name`, `Stage`) and the active record if the route has one. This is what makes
"what's the status of this?" answerable without a tool call.

**Layer 2 — Pinned. Per capability pack, 100–400 tokens.**
The domain rules the pack cannot afford to get wrong, lifted from `CLAUDE.md` and the glossary. The
QS pack pins the variation lineage rules — one document, one number, read as `V72`, and the status
ladder. The Programme pack pins the EOT/NOD relationship and the JCT clause references. This is
cheap and it is the difference between the assistant sounding like it works here and sounding like
it read a wiki.

**Layer 3 — Retrieved. On demand, via tools.**
Everything else. The model asks. `RequestContextAssembler` is already the right shape for this and
should become a tool (`get_request_context`) rather than an always-on preamble.

**Anti-pattern, stated explicitly:** do not put the schema, the entity docs, or the API map in the
prompt. The tool catalogue *is* the API map, it is role-filtered, and it cannot go stale.

---

## 8. Conversation persistence and audit

### Widen the scope key

`AgentChatMessageEntity` and `AgentProposalEntity` are keyed on `RequestId`. The portal-wide
assistant is scoped to a route, a project, a record, or nothing at all. Add a conversation
aggregate and make the scope polymorphic — consistent with `RecordType`, which already enumerates
the record kinds:

```
ConversationEntity
  ConversationId (PK)      ScopeType (RecordType?)   ScopeId (string?)
  ProjectId?               CapabilityKey            StartedByEmail
  StartedAt                LastMessageAt            Title (model-generated, first turn)
```

Then re-key the message and proposal tables to `ConversationId` and keep `RequestId` as a nullable
denormalised column so the existing request-scoped workspace keeps working. Note `AgentChatMessage.Body`
is capped at 4000 chars — raise it, or tool results will be silently truncated.

### Three things must be recorded

1. **The turn** — user message, assistant message, and the tool calls between them, in order.
2. **The tool execution** — written by the endpoint that ran it, through the existing `AuditTrail`,
   with the conversation id in the payload. This is the evidential record; the transcript is not.
3. **The decision** — who confirmed a proposal, when, and what they changed before confirming.
   `AgentProposalEntity` has `DecidedByEmail`/`DecidedAt` already; it needs an *edited values* column.

This is a construction contract system. When a variation is approved or a notice is issued, "the
assistant suggested it and Nigel confirmed it at 09:14, having changed the value from £48,320 to
£47,900" has to be reconstructable years later. Design for the dispute, not the demo.

### And accepting a proposal must do something

Close the gap in `DecideAgentProposalHandler`: on Accept, deserialise `StructuredJson` into the
declared `ContractType` and dispatch it through `ICommandHandler`. One switch, in one place, with
the tool descriptor as the map. Without this the human-in-the-loop model is decorative.

---

## 9. ADR-006 — Email stays draft-only

C3 is not a limitation to work around. It is the correct behaviour, enforced by a permission that
cannot be bypassed by a bug or a prompt injection.

Every agent-authored email becomes an Outlook draft via the existing `CreateDraftAsync` /
`CreateReplyDraftAsync` path, tagged to the record, CC'd to the projects mailbox, with a
`DraftCreated` audit row carrying the Graph id and `WebLink`. The human opens it in Outlook, reads
it, and sends it. That is the approval step, in the tool they already use, and it costs us nothing
to build because six callers already do it.

The brief's "draft emails to people to move things into awaiting client response" maps onto this
exactly. Keep it.

**Corollary for prompt injection.** Emails are untrusted input — they are written by clients,
architects and subcontractors, and the agents read them. Two mitigations, both structural rather
than prompt-based: email content is always delimited and labelled as third-party data in the
prompt, and **no tool that acts on the outside world is `Direct`**. The worst a malicious email can
achieve is a proposal a human declines.

---

## 10. The turn loop, concretely

One new API endpoint carries the whole interactive design.

```
POST /api/ai/turn
```

Request: the conversation id, the message list (user/assistant/tool_result), the capability key, and
the current scope (route, project id, record id).
Response: either an assistant text block, or one or more `tool_use` blocks.

Request: **the conversation id and (on the first call) the new user message** — never the history.
Response: the assistant's text, plus a `Status` of `Complete`, `AwaitingClient` (a UI tool to run),
or `AwaitingApproval` (a proposal card to render).

The endpoint's job, per call:

1. Resolve the signed-in user (existing gate).
2. Load the conversation from the database — messages, capability pack, scope.
3. Assemble the tool catalogue for `(pack, roles, scope)` and the ambient + pinned context.
4. Make **exactly one** Claude call with tools declared.
5. For each `Read` tool the model asked for: run its `*Authorisation` class against the resolved
   user, dispatch the handler, persist the result as a tool-result message.
6. For a `Write` tool: persist an `AgentProposalEntity` and return `AwaitingApproval`. **Stop.**
7. For a `Ui` tool: return it and `AwaitingClient`. **Stop.**
8. Otherwise return the assistant text and `Complete`.

The client then loops: while the status is not `Complete`, either execute the UI tool and
`POST /api/ai/turn/continue` with its advisory result, or render the proposal card and wait for a
human. Confirming a proposal dispatches the real command and continues the loop; declining it
appends that fact and continues, so the model knows it was refused.

Two guards worth writing on day one: a **step budget** per user turn (10 is generous; tell the model
its budget so it can plan), and a **cost ceiling** per conversation, surfaced in the panel. The cost
strip already promises the user this is a billed feature — honour that with a real number.

`IClaudeClient` needs replacing, not extending: a `ClaudeConversationClient` taking a message list,
a tool list and a system prompt, returning typed content blocks. Keep the old single-shot
`CompleteAsync` — `PrepareVoqDraftHandler` and `ExtractQuoteFromMessageHandler` work and should not
be disturbed.

---

## 11. What this does not solve

Stated plainly so nobody discovers them late. Detail and options in `02-gaps-and-roadmap.md`.

- **Bluebeam is not integrated** (licence and appetite exist; the code does not). Handled by the
  not-configured tool pattern below rather than by hiding the capability.
- ~~There is no contract entity.~~ **Built** — `ProjectContractEntity` and the `ProjectContracts`
  slice. Contract sum, dates, LAD rate, retention, notice periods and the OH&P standing rules are now
  facts about a project, and `get_project_contract` is a first-class read tool. See §13.
- **No timer infrastructure exists.** Zero `TimerTrigger`s in the repository. The worker's DI wires
  five services and knows nothing about Claude, Xero, blobs or the CQRS handlers. Both 9am runs are
  greenfield, and the worker's hosting plan is not declared in any infra script.
- **`EotEntity` has no link to its request.** EOT and NOD are `RequestEntity` rows; the CVR-side
  `EotEntity` is unrelated, with no join column. Granting an EOT moves no programme date.
- **Bid package lines carry no price.** `BidPackageLineItemEntity` has quantity, unit, trade and cost
  code — pricing lives only on `QuoteLineItemEntity`. A package has no budget of its own.
- **Anonymous bid submission has no auth path.** Every endpoint resolves a session cookie.

---

## 12. ADR-007 — Missing integrations are declared, not hidden

A tool whose backing integration is not wired up is still **declared to the model**, and returns a
structured not-configured result rather than being absent from the catalogue.

```csharp
public sealed record ToolResult(
    bool Ok,
    object? Value,
    ToolFailure? Failure);

public sealed record ToolFailure(
    string Kind,            // "not_configured" | "not_found" | "forbidden" | "invalid" | "upstream"
    string Message,         // what the model tells the user
    string? OperatorAction);// what the team must do, surfaced to the operator, not the user
```

So `bluebeam_extract_quantities` exists from day one and returns:

```json
{ "ok": false,
  "failure": { "kind": "not_configured",
    "message": "Bluebeam is not connected, so I can't take quantities off the drawings. I can scope this package from the correspondence and the drawing register instead.",
    "operatorAction": "Configure Bluebeam__ClientId / Bluebeam__ClientSecret and implement IBluebeamClient." } }
```

Three reasons this beats omitting the tool.

**The user gets a true answer instead of a confabulated one.** A model with no takeoff tool asked to
scope a package from drawings will do its best from the filenames and sound confident. A model that
*tried* and was told the integration is off says so.

**It surfaces the gap as demand.** Every not-configured result is logged with the conversation and
the project. After a fortnight you know how often Bluebeam was actually reached for, which is a
better prioritisation signal than an argument about it.

**Degradation is designed rather than emergent.** The `message` names the fallback, so the model
offers the correspondence-based route instead of stalling.

The same pattern covers Xero and Brave when unkeyed — both already have null-object clients that
throw; wrap them to return `not_configured` instead. Note `NullProjectContractBlobStore` follows the
house convention of throwing with the setting name in the message; the tool layer is where that
becomes a structured failure.

---

## 13. ADR-008 — The contract is a fact about the project

`ProjectContractEntity` (one row per project, unique on `ProjectId`) now holds what was previously
scattered across frozen per-claim copies: the form and edition, the parties, the contract sum, the
LAD rate, possession and completion dates, retention pre- and post-completion, the defects period,
the payment mechanism (application cut-off day, payment-notice days, pay-less days, final date), and
the OH&P standing rules.

The OH&P percentages are on the contract, not in configuration, deliberately. They are contract
terms — argued from the Contract Particulars and different per project — and the QS skill pack
treats them that way (10% direct, nil on omission, 5% attendance, daywork percentages). Hard-coding
them would make the assistant confidently wrong on any project let on different terms.

Three consequences worth naming:

1. **`get_project_contract` is the QS pack's most important read tool**, and it must be called before
   any clause is cited or any OH&P figure is applied.
2. **`ProjectContract.IsAmended` gates clause citation.** A bespoke or amended form does not map to
   the standard, and `BespokeDeviations` must be read before a clause number is quoted. The system
   prompt says: on an amended form, cite from the deviations or say the clause needs checking.
3. **Notice deadlines become computable.** `PaymentNoticeDays`, `PayLessNoticeDays` and
   `FinalDateForPaymentDays` turn "flag every notice deadline before it lapses" from an aspiration
   into arithmetic — which belongs in code, not in the model.

The existing frozen copies (`ValuationClaimEntity.ContractSum`, `LadClaimEntity.RatePerWeek`) stay.
They are deliberate snapshots of what was true when a claim was raised. The contract row is the
source they should be *taken from*; a backfill and a divergence check are follow-on work.

---

## 14. Build order

Each phase ends somewhere shippable. Do not start phase N+1 with phase N half-landed.

**Phase 0 — Foundations.** `ClaudeConversationClient` with tool support. `ToolDescriptor` +
generated JSON schema. `POST /api/ai/turn`. `ConversationEntity` and the re-keyed message tables.
Five read tools and `navigate_to`. *Ends with:* the assistant can answer questions about the project
you are looking at and move you around the portal. No writes.

**Phase 1 — Proposals.** `ModalCatalog` and `open_modal`. The proposal card. Accept-dispatches-command
in `DecideAgentProposalHandler`. The first three write tools, all `Proposal`. *Ends with:* the
assistant can fill in a dialog for you and you press the button.

**Phase 2 — The QS pack.** The pack model itself, the Orchestrator, `switch_capability`. QS tools
over variations, requests and valuations. *Blocked on the QS script, and on a contract entity for
terms-aware answers.*

**Phase 3 — Autonomous runs.** Worker DI expansion, the first `TimerTrigger`, the system pseudo-user
and its capability set. Request Agent at 09:00 drafting chasers. *Confirm the worker's hosting plan
and set `WEBSITE_TIME_ZONE` before writing a line of this.*

**Phase 4 — Bid Package pack.** Blocked on Bluebeam or a CSV path, and on a decision about
anonymous bid submission.

**Phase 5 — Programme pack.** Blocked on the EOT/NOD linkage and a contract entity.

Timesheet and Mailbox Triage can be slotted in wherever they fit — Timesheet is the smallest useful
pack and would make a good Phase 1 proof; Mailbox Triage is a revival of a prompt that already
exists and was removed.
