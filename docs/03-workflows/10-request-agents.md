# Workflow 10 — Request Agents (AI agent system)

> Status: **v1 framework, stub agents.** This document describes the agent system layered on top of the
> Requests domain. The three agents ship as stubs that decline to complete their work; the full
> structure (assignment, global watch queue, chat, Claude structured proposals, human-in-the-loop
> review, and close-gating) is real and production-shaped.

## 1. Purpose

A **request** is the unit of work that already flows through JPMS — raised directly or created from the
`projects@` mailbox triage. The agent system lets a human apply one or more **agents** to a request.
Each applied agent:

1. is added to a **global queue of requests being watched** (one row per request × agent),
2. can be talked to in a **per-agent chat** (the user picks the agent from a dropdown),
3. can **analyse** the request — reading all of the request's emails, messages and files — and return a
   **structured object** (a *proposal*) for a human to accept or reject, and
4. has a **completion opinion**: a request **cannot be closed** until *every* applied agent agrees its
   work is complete.

Because the three agents ship as stubs, every applied agent currently reports "not implemented, work
not complete", so a request with any agent applied cannot yet be closed through the agent gate — exactly
the behaviour requested for this iteration.

## 2. The three agents

| Key | Agent | Discipline | When implemented it will… |
|-----|-------|------------|---------------------------|
| `bid-packages` | Bid Packages Agent | Procurement | Issue bid packages, select bids, raise purchase orders, hand off to scheduling. |
| `scheduling` | Scheduling Agent | Programme | Schedule all work, issue EoT and NoD notices. |
| `valuations` | Valuations Agent | Commercial | Create & confirm variation-order quotes, pull latest financials. |

All three derive from `StubAgent`, which returns the not-implemented chat reply, an `Unavailable`
proposal, and `IsComplete = false` for the close gate.

## 3. Data model (new)

Three EF entities (`api/Data/Entities/AgentEntities.cs`), string PKs, enums as `int`, no FK constraints
(by-id only) — matching every other JPMS table.

- **`RequestAgentEntity`** (`RequestAgents`) — an agent applied to a request (the watch row).
  `RequestAgentId` PK · `RequestId` · `AgentKey` · `Status` (`AgentAssignmentStatus`) · `IsPrimary`
  (the request's lead agent) · `StatusMessage` · `AssignedByEmail` · `AssignedAt` · `CompletedAt?`.
- **`AgentChatMessageEntity`** (`AgentChatMessages`) — the per-(request, agent) conversation.
  `MessageId` PK · `RequestId` · `AgentKey` · `Role` (`AgentChatRole`: User/Agent/System) · `AuthorEmail`
  · `AuthorName` · `Body` · `PostedAt`.
- **`AgentProposalEntity`** (`AgentProposals`) — a persisted structured proposal from an agent.
  `ProposalId` PK · `RequestId` · `AgentKey` · `Status` (`AgentProposalStatus`:
  Pending/Accepted/Rejected/Superseded/Unavailable) · `Summary` · `StructuredJson` (nvarchar(max)) ·
  `Rationale?` · `CreatedAt` · `DecidedByEmail?` · `DecidedAt?`.

Migration `…_AddRequestAgents` creates the three tables plus supporting indexes; applied automatically by
`JpmsContext.Database.MigrateAsync()` on API startup.

## 4. Backend (`api/Features/Agents`)

Mirrors the Requests vertical-slice convention exactly.

- **`IRequestAgent`** — the agent abstraction: `Key`, `DisplayName`, `Discipline`, `Summary`,
  `IsImplemented`, plus `RespondAsync` (chat), `AnalyseAsync` (structured proposal), and
  `EvaluateCompletion` (close-gate opinion). `StubAgent` is the not-implemented base; the three agents
  subclass it.
- **`AgentRegistry`** — resolves agents by key and lists descriptors. Registered as a singleton; each
  `IRequestAgent` registered as a singleton.
- **`RequestContextAssembler`** — gathers the request header, the in-app/email conversation
  (`RequestMessages`) and the originating intake emails into a single text context the agent hands to
  Claude (`IClaudeClient`, reused from `Features/Ai`). Stubs ignore the context but it is built so the
  real agents drop straight in.
- Commands: `AssignAgent`, `RemoveRequestAgent`, `SendAgentMessage`, `RunAgentAnalysis`,
  `DecideAgentProposal`, `AttemptCloseRequest` (each: Endpoint + Handler + Authorisation + Validation).
- Queries: `ListAvailableAgents`, `ListRequestAgents`, `ListAgentChat`, `ListAgentProposals`,
  `ListAgentQueue` (each: Endpoint + Handler).

**Close-gating** lives in `AttemptCloseRequestHandler`: it loads every `RequestAgent` for the request,
asks each agent `EvaluateCompletion`, and if *any* is incomplete returns
`RequestCloseOutcome(Closed: false, BlockingAgents: …)` **without** closing. Only when all agree does it
set `RequestStatus.Closed`. With stub agents this always blocks while agents are applied.

**Structured proposals** reuse the existing Claude pattern: a system prompt instructs JSON-only output,
the handler extracts the first balanced `{…}` object and stores it as `StructuredJson`. A human accepts
or rejects via `DecideAgentProposal` — nothing in a proposal takes effect until accepted.

Authorisation: agent actions are open to `Director`, `ProjectManager`, `Estimator` (QS), `SiteManager`
(and admins, who carry every role server-side) via a shared `AgentRoles` set.

## 5. Front-end (`jpms`)

- **`IAgentDesk` / `HttpAgentDesk`** — the store facade (same shape as `IRequestRegister`), wired through
  `AgentsReadModel` + `IQueryClient`/`ICommandSender` and the route table (`AgentsRouteRegistration`).
- **Agent selection** — on the request detail page, an "Agents" panel lists applied agents with their
  completion state and an "Apply agent" dropdown (the diagram's option screen).
- **Agent chat** (`AgentChat.razor`) — a per-request chat with an **agent dropdown**; messages go to the
  selected agent and the agent's reply is appended. A "Run analysis" action produces a proposal.
- **Proposal review** (`AgentProposalReview.razor`) — the human-in-the-loop screen: shows the structured
  object with Accept / Reject.
- **Global queue** (`/agents` page, `AgentQueue.razor`) — every watched (request, agent) pair across the
  portfolio, filterable by discipline. Added to the desktop nav for internal commercial roles.
- **Close** now routes through `AttemptCloseRequest`; if blocked, the UI lists the agents that still have
  outstanding work and keeps the request open.

## 6. Future work (out of scope this iteration)

Replace each `StubAgent` with a real implementation that calls `AnalyseAsync` against Claude and returns
a discipline-specific structured object; extend `RequestContextAssembler` to fetch attachment **bytes**
from Microsoft Graph (today attachments are metadata-only); add agent-authored outbound emails.
