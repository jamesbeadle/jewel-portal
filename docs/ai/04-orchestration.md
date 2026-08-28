> **Superseded (2026-08-27).** The in-portal chat this document describes was retired in favour of the MCP connector — see [10-mcp-connector.md](10-mcp-connector.md). Kept as the historical record.

# Orchestration — how the assistant, the screen, the forms and the agents fit together

> Companion to `00-agent-architecture.md` (the ADRs), `01-agent-specifications.md` (the packs) and
> `02-gaps-and-roadmap.md` (the blockers). This document answers one question those three left
> open: **how does the turn-based conversation, the per-screen context, the form-filling and the
> agent roster compose into one system** — including the fourteen process agents in
> `JBB-Agent-System.docx`, which were drawn up in business language and have never been reconciled
> with the code.
>
> Status: the turn loop is **built and live**. This document is partly an as-built record of it and
> partly the design for the orchestration layer that sits on top. Terminology follows `CLAUDE.md`:
> programme, valuation invoice, variation (one document, one number, read as `V72`). "AI" in a
> record context means Architect's Instruction.
>
> The visual version of this document is `04-orchestration-map.svg`, alongside it.

---

## 0. The one-paragraph version

There is **one conversation, one turn loop, one assistant** — and it already runs in production
code (`AiTurnRunner`, `ChatPanel`). An "agent" — the Variations Agent, the RFI Agent, whatever the
roster ends up calling them — is **not a second participant and not a second process**. It is a
*configuration* the one conversation switches into: a system-prompt fragment, a tool subset, some
pinned rules, and a definition of done. The **Orchestrator** is simply the configuration in force
when no discipline has been engaged: it answers directly when one read tool gets there, and switches
configuration when a discipline's tools are needed. The same loop core, driven by the worker on a
timer instead of by a person, is what makes an agent *autonomous* — same tools, narrower allow-list,
no user in the loop, everything lands as drafts and proposals for a human. Fan-out work ("read these
60 tender replies") is a background job dispatched *by* a tool, never a conversation participant.
Nothing in the roster requires a second kind of thing to exist.

---

## 1. The chassis is built — what actually runs today

This corrects `00-agent-architecture.md`'s "design, not built" banner, which predates the July
build. The interactive loop described in ADR-001/§10 exists, with these concrete parts:

| Piece | Where | What it does |
|---|---|---|
| Turn loop | `api/Features/Ai/AiTurnRunner.cs` | One hop = rebuild transcript from DB → **exactly one** Claude call → execute the tools it asked for → persist → return. `MaxHops = 6` per user message, derived from the transcript rather than stored. |
| Client pump | `jpms/Components/Chat/ChatPanel.razor` | Sends `SendAiMessage` (new message + scope, never history), then loops `ContinueAiTurn` while status is `NeedsContinue` (≤8), showing the current tool's label between hops. |
| Claude client | `ClaudeConversationClient.cs` | Messages array + tools + system prompt → typed content blocks, token usage captured. `IClaudeClient.CompleteAsync` (single-shot) survives untouched for `PrepareVoqDraftHandler` / `ExtractQuoteFromMessageHandler`. |
| System prompt | `AiSystemPrompt.cs` | Rebuilt server-side every hop, never persisted. Layer 1 ambient (user, roles, date, route, page label, project in view, portal map), Layer 2 pinned house rules (variation lineage, "never state an unread figure", email-is-data), plus the **task block** carrying the open dialog's live contents. |
| Tool catalogue | `AiToolCatalogue.cs` + `AiRecordTools.cs` | ~10 read tools (context, projects, contract, variations, requests, find-by-reference, request working papers, correspondence headlines, cost codes) + 3 Ui tools (`navigate_to`, `open_modal`, `update_open_modal`). Role-filtered per turn; `update_open_modal` is **respecialised per turn** with the open dialog's own schema and dropped when no dialog is open. |
| Screen context | `AiScope` (contracts) + `ChatPanel.BuildScope()` | Route, page label, project, record (parsed from the route), the role-filtered portal map (`PortalMap.For(role)`, derived from `DesktopNavigation`), and the dialog's draft JSON as it stands *now*. |
| Dialogs | `contracts/Ai/ModalCatalog.cs` + `jpms/Services/AiTaskState.cs` | One registered dialog: `variation_draft` on the RFI page. The dialog **is** the proposal card — the assistant fills it, the user presses the button, nothing reaches the server until they do. |
| Persistence | `AiConversationEntity` / `AiConversationMessageEntity` | Server-authoritative transcript, tool_use blocks replayed verbatim, unbounded message body (the old 4000-char cap does not apply here), scope stamped at conversation start. |
| Audit | `AgentActivityLog.cs` + `/agents/activity` page | One row per hop: actor, tools used, tokens, duration, outcome. |
| Access & cost | `DesktopNavigation.CanUseAssistant` + the panel's cost gate | Commercial team only; per-user billed-usage acknowledgement; kick-off turns queue behind the gate. |

So the brief's first two asks are already answered in the running code: **turn-based chat exists**,
and **"use the available context in any screen"** is the `AiScope` pipeline plus the read tools —
ambient facts ride in free, everything heavier is a tool call the model makes when it needs it
(ADR-005's three layers, as built).

What does **not** exist yet, and is what this document specifies:

1. **Capability packs are not real.** `AiConversationEntity.CapabilityKey` exists and is hardcoded
   `"orchestrator"`. There is no pack model, no registry, no `switch_capability`, and the prompt
   and catalogue are assembled the same way for every conversation.
2. **There is no server-side write path.** Every mutation today goes through the one registered
   dialog. There is no proposal entity for the new conversation tables, no proposal card in the
   panel, and nothing that dispatches a confirmed proposal through a command handler. (The old
   `AgentProposalEntity` + `DecideAgentProposalHandler` pair from the parked request-workspace
   still flips a status field and executes nothing — ADR gap #3 is still open.)
3. **No autonomous runtime.** Zero `TimerTrigger`s; the worker knows nothing about Claude or the
   CQRS handlers (unchanged from `02-gaps-and-roadmap.md` §3).
4. **`ask_user`, `highlight` and `set_filter` are still design.**
5. **One transcript-budget defect** — see §6, it needs fixing before conversations get long.

---

## 2. The orchestration model

Five layers. Each one is small; the power is that they compose without any layer knowing about the
ones above it.

```
┌──────────────────────────────────────────────────────────────────────┐
│  E. Runtimes        chat panel (user pumps)  │  worker timer (no user)│
├──────────────────────────────────────────────────────────────────────┤
│  D. Packs           orchestrator │ variations │ bid & award │ …       │
│     (the "agents")  prompt fragment + tool subset + pinned + done     │
├──────────────────────────────────────────────────────────────────────┤
│  C. The turn loop   AiTurnRunner — one Claude call per hop  [BUILT]   │
├──────────────────────────────────────────────────────────────────────┤
│  B. The tool plane  Read (free) │ Write (proposal) │ Ui (browser)     │
├──────────────────────────────────────────────────────────────────────┤
│  A. The portal      CQRS handlers, RBAC gates, ModalCatalog, routes   │
└──────────────────────────────────────────────────────────────────────┘
```

### 2.1 The orchestrator is a pack, not a router service

Per ADR-004, kept deliberately: there is no dispatcher process, no hand-off, no child
conversations. The Orchestrator is the **resting configuration** of every conversation — the
`CapabilityKey` a conversation is born with. Its job is two things in strict order: answer directly
when one read tool gets there ("what's the value of V72" must not become a workflow), and engage a
discipline pack when the *tools* of that discipline are needed — announced in one clause, never a
paragraph. Switching packs changes the prompt fragment and the tool subset **within the same
conversation**; history survives; the model is told in-band that its remit changed.

Pack selection, in precedence order (unchanged from the ADR, now pinned to mechanisms):

1. **Explicit** — the user asks, or the model calls `switch_capability(key, reason)` — the one
   genuinely safe `Direct` write, since its blast radius is the conversation's own configuration.
2. **Contextual** — the route sets the *initial* key at conversation start:
   `/projects/{id}/variations/*` starts in the variations pack, `/projects/{id}/programme` in the
   programme pack. `SendAiMessageHandler.LoadOrStartAsync` is where this lands (it already stamps
   scope; today it stamps the constant).
3. **Fallback** — orchestrator.

### 2.2 A pack is a record in a registry

```csharp
// contracts/Ai/CapabilityPack.cs — mirrors ModalCatalog's shape and registration style
public sealed record CapabilityPack(
    string Key,                          // "orchestrator", "variations", "bid-award", …
    string DisplayName,                  // what the panel banner and the activity log show
    string PromptFragment,               // the discipline's voice, method and hard rules
    IReadOnlyList<string> ToolNames,     // subset of AiToolCatalogue.All, by name
    IReadOnlyList<string> ModalKeys,     // subset of ModalCatalog.All this pack may open
    IReadOnlyList<Role>  AvailableTo,    // packs a role can engage — separate from CanUseAssistant
    string DoneMeans);                   // completion policy, rendered into the prompt

public static class CapabilityPackCatalogue   // same pattern as ModalCatalog: explicit opt-in
{
    public static IReadOnlyList<CapabilityPack> All { get; }
    public static CapabilityPack Orchestrator { get; }
    public static CapabilityPack? Find(string? key);
    public static IReadOnlyList<CapabilityPack> For(IEnumerable<Role> roles);
}
```

Per-turn assembly then becomes one line of composition in `AiTurnRunner.RunHopAsync`:

- **catalogue** = `AiToolCatalogue.For(user, scope)` ∩ `pack.ToolNames`
  (+ `switch_capability`, always) — the existing role filter and modal specialisation keep
  running underneath, so ADR-002's rule survives: a tool the user could not invoke is never
  described, whatever the pack says.
- **prompt** = shared preamble (the current `AiSystemPrompt` layers) + `pack.PromptFragment`
  + pack-specific pinned rules. The task block and the Never rules are shared and non-negotiable —
  a pack can add rules, never subtract them.
- **`ModalCatalog.For(roles)`** additionally intersects `pack.ModalKeys`, so the variations pack
  offers the variation dialog and the timesheet pack does not.

Two consequences worth naming. *Names are cheap*: the fourteen business agents map onto pack keys,
and renaming an agent is renaming a registry entry — nothing else couples to it (the roster can be
fine-tuned indefinitely without touching the architecture). *The orchestrator cannot leak*: since
it is just a pack whose tool list is the read set + Ui + `switch_capability`, there is no state in
which "no pack" means "all tools".

### 2.3 The tool plane: read is free, write is a proposal, Ui is the browser's

Unchanged from ADR-002/003, restated as the contract every pack inherits:

- **Read** executes immediately, under the caller's own session (interactive) or the pseudo-user
  (autonomous). Worst case is a wasted call.
- **Ui** is returned to the browser and executed there: `navigate_to`, `open_modal`,
  `update_open_modal` today; `ask_user`, `highlight` next. Client results are advisory only.
- **Write** never executes from the model. It lands one of two ways:
  - **A registered dialog exists** → the write *is* the dialog: `open_modal` +
    `update_open_modal`, the user presses the button, the normal endpoint runs. This is the
    preferred path for anything a page already has a form for — the proposal card is a form the
    user already knows, pre-filled.
  - **No dialog** (status changes, autonomous output, batch actions) → a **proposal row**:
    the tool persists `AiProposalEntity` and the turn returns `AwaitingApproval`. The panel (or
    the queue page, for autonomous runs) renders the card — exact command, exact values, Confirm /
    Edit / Discard. Confirm dispatches the declared contract type through the real
    `ICommandHandler` with its `*Authorisation` gate; the outcome (and any edited values) is
    appended to the conversation so the model knows what actually happened.
  - **`Direct`** stays an opt-in allow-list per tool, justified in a comment: `switch_capability`,
    and later autonomous tagging. Nothing that touches money, contract status or a third party.
    Ever. The email rule (ADR-006) also stands: agent-authored email is an Outlook draft, and no
    agent tool is wired to `SendDraftAsync`.

### 2.4 "Build any form on any screen" = registering dialogs

The generalisation the brief asks for is **not** a form generator. It is the `ModalCatalog`
pattern applied dialog by dialog, deliberately: registering a `ModalDescriptor` is the explicit
opt-in that makes a form reachable, and each field's description is where that form's house rules
live (once). The recipe per dialog is now proven by `variation_draft` and costs roughly:

1. A `ModalDescriptor` in `ModalCatalog` — key, purpose, route template, roles, fields.
2. The owning page honours `?openModal={key}` on arrival and starts an `AiTask` when its dialog
   opens (`AiTaskState.Start`), with `ChatAware` + `DismissOnOverlayClick=false` on the `Modal`.
3. The dialog merges `AiTasks.ApplyFromAssistant` field-by-field — never replaces — and validates
   exactly as if the user had typed it (cost codes only on exact master match, etc.).

Candidate registrations, in rough order of value: raise request (RFI/NOD/EOT — the request dialog
already exists), create bid package / invite subcontractors, submit timesheet, record contract
amendment, draft request email (compose is a dialog too). Each is one descriptor plus page wiring
— no prompt changes, no new tool names, because `update_open_modal` respecialises itself.

### 2.5 Runtimes: the same loop, three drivers

| Driver | Identity | Capability | Output |
|---|---|---|---|
| **Chat panel** (built) | The signed-in user's session cookie; tools run behind their own RBAC | The pack in force | Answers, navigation, filled dialogs, proposals |
| **Worker timer** (Phase: autonomous) | System pseudo-user, `ActorEmail = projects@jewelbb.co.uk`, following `MailboxActionWorker`'s precedent — no service-principal path into the API is invented | A *named, narrower* allow-list per scheduled run — read widely, draft email, propose; `Direct` only for reversible, audited tagging | Outlook drafts + proposal rows onto a human-in-the-loop queue |
| **Background job** (dispatched by a tool) | The dispatching context, recorded on the job | Bounded fan-out analysis — drawings compare, tender-reply ranking | A proposal (never a chat message; jobs are not participants) |

The line stands: **conversation is single-threaded; analysis fans out.** The worker runs are not
"other agents talking" — they are the same packs on a clock, with no one to ask, which is exactly
why their write mode is proposals-only and their `Direct` set is nearly empty.

---

## 3. Mapping the fourteen JBB agents onto the architecture

The `JBB-Agent-System.docx` roster is drawn around *processes* and that instinct is right — it is
the same instinct as packs-not-roles. But the fourteen are business units of accountability, not
units of software. Each maps onto one or more of the three mechanisms above. Names are the
roster's; keys and groupings can be fine-tuned later without structural cost (§2.2).

| # | JBB agent | Mechanism today's architecture gives it | Notes / blockers |
|---|---|---|---|
| 1 | RFI Agent | Pack (requests scope) + mailbox triage feeds it | Read tools exist; raise-request dialog to register |
| 2 | Variations Agent | **Pack — furthest along.** `variation_draft` dialog, QS read tools, contract tool all live | The pilot: already half-built. `approve_variation` stays human, on the page |
| 3 | Delay & Claims (NOD/EOT) | Pack + 09:00 sweep (worker) | Blocked on EOT↔request linkage (`02` §4); NOD/EOT are requests, raise via dialog |
| 4 | Valuations & Final Account | Pack (valuation scope) | Valuation-position read tool to add; claims stay human-driven |
| 5 | Programme Agent | Pack wrapping `SchedulingAgent` as a read tool + weekly worker run | Deterministic maths stays deterministic; blocked on timer infra |
| 6 | Bid & Award Agent | Pack + `start_analysis` background jobs (reply ranking) | Supplier search already works; scope-from-correspondence now, Bluebeam later (`02` §1 option A) |
| 7 | Subcontractor Commercial | Pack + monthly worker run | Three-signature payment gate is untouchable — proposals only |
| 8 | Technical & Handover | Pack + background jobs (drawing revision compare) | Drawing pipeline placeholders exist; analysis is a job, not a turn |
| 9 | CVR/FFA & Cashflow | Mostly **worker runs** producing report proposals; thin interactive pack for questions | Deterministic renders in code; the model narrates, never computes |
| 10 | Pre-construction & Estimating | Pack (tender scope) | Contract-terms review needs `get_project_contract` — built |
| 11 | Site Diary & Quality | Pack behind a **second, narrower chat entry point** for site staff | Same lesson as the timesheet spec: "who can open chat" ≠ "which packs" |
| 12 | H&S Incident | Pack, alert-heavy; the statutory cascade is a workflow, not a conversation | Human gates everywhere; agent drafts and chases, never decides |
| 13 | Weekly Cadence | Almost purely **worker runs** + proposals; not really conversational | A scheduler with a voice. Build late |
| 14 | Gate & Governance | The **audit surface itself**: activity log + proposal decisions + thresholds | Mostly already falls out of §2.3's records; add threshold checks in code |

Plus the front door: **Mailbox Triage** is a worker run on a 5-minute timer (prompt already exists,
retired 2026-07-22), whose one `Direct` write is reversible, audited tagging. One message can
legitimately raise proposals into several processes at once — which in this architecture is just
several proposal rows, not several agents talking.

The reading to take away: **nothing in the fourteen needs a new kind of thing.** Interactive
disciplines are packs; scheduled duties are worker runs; heavy analysis is background jobs; every
human gate in the docx becomes either a dialog button, a proposal Confirm, or a page the assistant
navigates to and stops.

---

## 4. The turn, end to end (as-built + the two missing statuses)

```
user types / task kicks off
  └─ SendAiMessage { conversationId?, message, scope }        ← scope: route, page, project,
       server: persist user row                                  record, portal map, dialog JSON
       ┌────────────────── one hop (AiTurnRunner) ──────────────────┐
       │ load transcript from DB                                    │
       │ resolve pack ← conversation.CapabilityKey        [NEW]     │
       │ build system prompt (shared + pack + task block)           │
       │ build catalogue (role ∩ scope ∩ pack)                      │
       │ ONE Claude call                                            │
       │ for each tool_use:                                         │
       │   Read  → authorise as caller → execute → persist result   │
       │   Ui    → collect for the browser                          │
       │   Write → persist AiProposalEntity → AwaitingApproval [NEW]│
       │ persist assistant row (+tool_use blocks), log activity     │
       └────────────────────────────────────────────────────────────┘
  ← AiTurnResult { status, newMessages, uiActions, steps }
  client: fold messages, run Ui actions (navigate / fill dialog / ask_user)
      status NeedsContinue   → ContinueAiTurn (≤8 pumps, ≤6 hops server-side)
      status AwaitingApproval→ render proposal card; Confirm dispatches the
                               command, appends outcome, continues the turn
      status Complete        → done
```

Statuses today: `Complete | NeedsContinue | Truncated | Unavailable`. Add `AwaitingApproval`
(proposal card) — `AwaitingClient` is unnecessary as built, because Ui actions already ride along
on every result and the pump continues regardless.

---

## 5. Build order (the turn-based conversation first, agents after)

Ordered so the conversation chassis is complete and trustworthy before any roster work. Each step
ships alone.

**Step 1 — repair the transcript budget (small, do first).** See §6. Without it, long drafting
conversations re-pay the full email thread every turn.

**Step 2 — make `CapabilityKey` real.** `CapabilityPack` + catalogue + `switch_capability` +
route-based initial key + prompt/catalogue intersection in the runner (§2.2). Ship with exactly
two packs — orchestrator and variations — where variations is today's behaviour (its dialog, the
QS read tools) under its own key. Nothing observable changes for users; the seam now exists.

**Step 3 — the proposal write path.** `AiProposalEntity` keyed on `ConversationId` (contract type,
values JSON, proposed/decided/edited-values columns), the `AwaitingApproval` status, the card in
`ChatPanel`, and Confirm-dispatches-through-`ICommandHandler`. First write tool through it:
`set_variation_status` (Quoting ⇄ Issued ⇄ Awaiting AI only) — small, reversible, real. The parked
`AgentProposalEntity`/`DecideAgentProposalHandler` pair is superseded by this and can be retired
with the rest of the old request-workspace surface.

**Step 4 — `ask_user` and `highlight`.** Two cheap Ui tools that make form-building conversational
(tappable choices instead of guessed fields) and make "control the site" visible (scroll-and-pulse
via the existing `jpmsFocusElement` + `.jpms-flash`).

**Step 5 — second and third dialogs.** Raise-request first (it serves RFI, NOD and EOT in one
descriptor), then bid-package-create or timesheet. Each proves the §2.4 recipe holds beyond the
dialog it was designed on.

**Step 6 — the autonomous runtime.** Confirm the worker's hosting plan and `WEBSITE_TIME_ZONE`
*first* (`02` §3), then: worker DI expansion, the pseudo-user, the first `TimerTrigger` — Mailbox
Triage at 5 minutes, reviving the retired prompt — and the proposals queue page (the `/agents`
route and `AgentQueue.razor` shell already exist to be repointed at the new table). The 09:00
request sweep follows.

**Step 7 onward — widen the roster.** Add packs in the order the mapping table's blockers clear:
delay & claims after the EOT linkage, bid & award analysis jobs, programme after timer + contract
facts are proven. This is where agent naming gets fine-tuned; nothing before it depends on names.

---

## 6. Defect noted while writing this: the transcript budget is dead code

`AiTranscript.cs` carries the two protections the architecture doc records as built —
supersede-stubbing of repeated `get_request_context` results and the 110k global budget that stubs
old tool rows oldest-first. **Nothing references it.** `AiTurnRunner` has its own private
`BuildTranscript` (which correctly replays `tool_use`/`tool_result` blocks — the thing
`AiTranscript` predates and cannot do), and it replays every tool row **verbatim, forever**. A
ten-turn drafting conversation re-sends the full RFI thread ten times: exactly the cost failure
the stubs were written to prevent, plus unbounded prompt growth toward the context limit.

Fix: fold `StubSuperseded` + `StubToBudget` into `AiTurnRunner.BuildTranscript` (they operate on
bodies and work unchanged over the block-replay shape), delete `AiTranscript`, and add the test
that would have caught this — a conversation with two `get_request_context` rows asserting the
older one is a stub. This is step 1 of §5 because every long conversation pays for it until then.

---

## 7. What this document deliberately does not change

- **ADR-001 (server authoritative, client pumps), ADR-002 (tools are contracts), ADR-003 (the
  dialog is the proposal), ADR-004 (packs, not processes), ADR-006 (draft-only email), ADR-007
  (missing integrations declared), ADR-008 (contract facts)** — all stand as written. This
  document is their composition, not their revision.
- **The blockers in `02-gaps-and-roadmap.md`** — Bluebeam, EOT linkage, bid-line pricing,
  anonymous bids, worker hosting — all still gate the packs they gated. The mapping table repeats
  them so nobody discovers them late.
- **The human gates in `JBB-Agent-System.docx`** — every one survives, mechanically: CEO batch
  sign-offs and three-signature payments are pages the assistant navigates to and stops; register
  ownership is proposals a named person confirms; RIDDOR calls and stop-work authority never enter
  the tool plane at all.
