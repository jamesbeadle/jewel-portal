> **Superseded (2026-08-27).** The in-portal chat this document describes was retired in favour of the MCP connector — see [10-mcp-connector.md](10-mcp-connector.md). Kept as the historical record.

# Gaps, Blockers and Build Order

> Companion to `00-agent-architecture.md` and `01-agent-specifications.md`.
>
> This document exists because the brief assumes several things exist that do not. Each is stated
> with its evidence, its impact on which agents, and the options — so the decision can be made
> deliberately rather than discovered halfway through an implementation.

---

## 1. Bluebeam is not integrated

**Severity: blocks the Bid Package pack's headline capability.**

The brief specifies *"Use Bluebeam API to automatically identify the work needed to be completed at
the measurement level"*. There is no Bluebeam integration in this repository. Not a stub — nothing.

**Evidence.** No client class, no config key, no package reference, no HTTP call. Every match is
prose:

- `README.md:105` lists Bluebeam integration as a *planned pass*.
- `docs/06-backlog/phase-1-vs-phase-2-proposal.md:25-28` moves **all four** Bluebeam integrations to
  Phase 2 — and the Phase 2 v1 is *"Markups List CSV import"*, with *"Markups API direct"* behind it.
- `api/Data/Entities/CoreEntities.cs:75-79` — `MetadataExtractedAt` and `AnalysedAt` exist on
  `DrawingRevisionEntity` as placeholders for a pipeline that was never built.
- `jpms/Pages/ProjectDrawingDetail.razor:74` — the button is hard-`disabled`, tooltip: *"The drawing
  analysis pipeline (Bluebeam metadata extraction + change analysis) is not connected yet"*.

There is no quantity takeoff anywhere in the system. BoQ entry is manual, and
`ValuationLineItemEntity` has no `BoqLineItemId` — the BoQ and the valuation report are disconnected
registers.

**Options.**

| | Approach | Effort | Gets you |
|---|---|---|---|
| **A** | Ship the Bid Package pack **without** measurement — scope from correspondence, drawings register metadata and trade knowledge | Low | ~⅔ of the brief. Recommended. |
| **B** | Markups List **CSV import** — a human exports from Bluebeam, the pack reads the CSV | Medium | Real quantities, human-triggered. The documented Phase 2 v1. |
| **C** | Bluebeam Studio API direct | High | The brief as written. Needs licence review, OAuth, and a session model that does not exist. |

**Recommendation: A now, B next.** Option A is a genuinely useful product — an assistant that scopes
a package from the email thread and the drawing set, finds subcontractors and drafts the invite. Do
not hold that behind an integration that is two phases away.

---

## 2. There is no contract entity

**Severity: blocks terms-aware behaviour in both the QS and Programme packs.**

The brief specifies *"Use linked contract for terms added to context"* for both packs. There is no
`ContractEntity`, no `ContractTermsEntity`, and no DbSet resembling one. JCT ICD 2024 appears only in
code comments.

**Where contract terms actually live today** — scattered as denormalised copies on operational
records, several of which can disagree with each other:

| Term | Actual home | Problem |
|---|---|---|
| Contract sum / revised sum | `ValuationClaimEntity.ContractSum` / `.RevisedContractSum` | **Frozen per claim.** Two claims can hold different contract sums. There is no project-level fact. |
| Retention % and release | `ProjectRetentionEntity` | The closest thing to a terms store — and it is retention-only. Also duplicated on `ValuationClaimEntity` and `ValuationEntity`. |
| LAD rate | `LadClaimEntity.RatePerWeek` | **Per claim.** Two claims can disagree about the contract rate. |
| Completion date | Nowhere | Only inferable as `max(ProgrammeTask.PlannedEnd)` on the latest baseline. |
| Payment terms | Nowhere | — |
| Defects liability period | `ProjectRetentionEntity.DefectsPeriodMonths` | Retention-framed, not contract-framed. |

**Impact.** An agent asked *"what does the contract say about X"* has nowhere to read from. Worse,
*"what is the contract sum"* has several answers that can differ, and the model will confidently
pick one. Given rule 2 in the cross-cutting section — never state a figure without having read it —
the honest v1 behaviour is for the assistant to **decline** contract-terms questions until this
exists.

**Options.**

- **A. Build `ContractEntity`** — one row per project holding form, contract sum, dates, LAD rate,
  payment terms, retention and defects period, with the operational records referencing it instead of
  copying it. Correct, and useful well beyond the agents. Medium effort; needs a migration and a
  backfill decision.
- **B. A read-only "contract facts" view** that picks a canonical source per term with documented
  precedence. Cheap, unblocks the agents, leaves the underlying disagreement in place.
- **C. Defer.** The packs decline terms questions and say why.

**Recommendation: C for Phase 2, A properly.** Do not let the agent programme drag a data-model
decision into being made quickly. But do not paper over it with B either — a "canonical source"
that silently picks between disagreeing numbers is exactly the failure this system cannot afford.

---

## 3. No scheduled-job infrastructure

**Severity: blocks both autonomous agents.**

**Evidence.** Zero `TimerTrigger` in the repository. The worker has exactly one function —
`MailboxActionWorker`, a `QueueTrigger` on `mailbox-actions` handling a single action type. The
timer extension package *is* referenced (`worker/Jewel.JPMS.Worker.csproj:20`), just unused.

**Four things to fix before the 09:00 runs.**

1. **Worker DI is bare.** `worker/Program.cs` wires `JpmsContext`, `MailboxIntakeOptions`,
   `IGraphMailClient`, `IMailboxQueue`, `IMailboxActionScheduler`. There is **no `IClaudeClient`, no
   CQRS handlers, no `AgentRegistry`, no blob store, no Xero client** — all of that is API-side. The
   options types would need linking into the worker csproj.
2. **The hosting plan is undeclared.** No infra script provisions the worker Function App; the
   workflow deploys to whatever `vars.JPMS_WORKER_APP_NAME` points at. If it is Consumption, the
   ceiling is 5 minutes default / 10 maximum, and `functionTimeout` is not set in
   `worker/host.json`. **Confirm this before committing to a long agent loop.**
3. **Queue redelivery will duplicate work.** `visibilityTimeout` is 5 minutes with
   `maxDequeueCount` 5. A handler that runs longer gets its message redelivered and the work runs
   twice. Decompose long runs across messages, or raise the timeout deliberately.
4. **`WEBSITE_TIME_ZONE` is unset**, so cron runs in UTC and "09:00" drifts an hour across BST. Azure
   Functions cron is **six fields** — `0 0 9 * * *`, seconds first.

**No blocker on identity.** The precedent exists: `MailboxActionWorker` talks to the database and
Graph directly and stamps `ActorEmail = projects@jewelbb.co.uk` on its audit rows (`:159`). Follow
that rather than inventing a service-principal path into the API.

---

## 4. EOT and NOD are split, with no link

**Severity: blocks the Programme pack's EOT handling.**

There are **two unrelated representations** of an extension of time:

- **`RequestEntity`** rows with `RequestType.ExtensionOfTime` (6) and `NoticeOfDelay` (3). The EOT
  request links back to its NOD via `RelatedNodRequestId` — the JCT ICD 2024 cl. 2.19 → 2.20
  relationship, correctly modelled.
- **`EotEntity`** (`CommercialDepthEntities.cs:56`) — a CVR-side row with `Reason`, `DaysGranted`,
  `CommercialRecovery`, `GrantedAt`.

**They have no join column.** Nothing connects a granted `EotEntity` to the request it arose from.
`GrantEot` and `ListEotsForProject` operate purely on `EotEntity`. And granting an EOT **moves no
programme date** — nothing writes `DaysGranted` back to `ProgrammeTaskEntity`.

**Impact.** The brief's *"Raise EOT and NOD records against a project"* and *"update weekly prelim
charges based on an EOT notification"* cannot be done coherently: the agent would raise a request,
and the commercial record it needs to update is unreachable from it.

**Fix.** Add `RequestId` to `EotEntity` and decide the direction of truth: does granting an EOT
create the commercial row, or does the commercial row reference an existing request? Then decide
whether granting shifts programme dates automatically or proposes a shift. Small migration,
meaningful design conversation — have it before Phase 5.

---

## 5. Bid package lines carry no price

`BidPackageLineItemEntity` has `Description`, `Unit`, `Quantity`, `Trade`, `CostCode`, `SortOrder`,
`Coverage` — and **no rate and no value**. Pricing lives entirely on `QuoteLineItemEntity`.

A bid package therefore has no budget or estimate of its own. The brief's *"seed the values for
variation orders"* has no source figure to seed *from* until quotes come back.

Two readings, and they need separating: seeding a variation from **returned quotes** works today.
Seeding one from an **estimate at scoping time** requires either a rate on the line or a lookup
against `RateEntity`. Decide which the brief means.

---

## 6. Anonymous bid submission has no auth path

The brief specifies *"submit your bid online, allowing an anon user to submit a bid package"*.

Every endpoint resolves a session cookie via `SignedInUserResolver`; there is no anonymous, token or
magic-link path. The subcontractor portal (`/portal/my/*`) uses
`SubcontractorScope.OwnSubcontractorId(user)` — a signed-in subcontractor, not an anonymous one.

**Options.** A signed tokenised link per `BidPackageRecipientEntity` (recommended — it is
per-recipient, revocable, expiring, and attributable, which an anonymous form is not); or extend the
subcontractor portal and require sign-in, which is more friction but no new surface.

Note this is a **new unauthenticated write surface on a system with no other one**. Rate limiting,
token expiry and payload validation are not optional extras here.

---

## 7. Smaller things worth knowing

- **Chat body is capped at 4000 characters** (`AgentChatMessageEntity`). Tool results will be
  silently truncated. Raise it before Phase 0 ends.
- **Enum values are load-bearing and several are unpinned.** `RequestStatus` has retired values
  (2, 3, 5) that must never be reused. Many enums have implicit values — inserting a member mid-list
  silently reinterprets stored rows. Pin them.
- **`docs/05-data-model/` is intent, not schema.** It documents ~25 entities with no table, and
  status ladders that match no enum in the code. Do not feed it to a model, and do not size work
  from it.
- **Two legacy mirrors must be written in step:** `TimesheetEntity.IsApproved` with `Status`, and
  `ProjectContactEntity.ReceivesRequests` with `Routing`.
- **Anthropic and Xero keys are in no infra script.** Only SQL, AAD, App Insights, ACS and
  drawings storage are set by `infra/azure-prod-setup-v2.sh`. The rest are manual, and the worker
  Function App is not provisioned by any script at all.
- **`api/README.md` is stale** — it documents the pre-CQRS pattern. `docs/cqrs/*.md` is accurate,
  except `06-api-surface.md`'s claim that auth comes from `X-MS-CLIENT-PRINCIPAL` (it is a session
  cookie).

---

## 8. Build order

Each phase ends somewhere shippable and demonstrable. The ordering is by **dependency and risk**,
not by value — the riskiest architectural assumption is proven first, cheaply.

### Phase 0 — Foundations *(no blockers)*

`ClaudeConversationClient` with tool support, replacing nothing (`CompleteAsync` stays — two
handlers depend on it). `ToolDescriptor` and schema generation from the contract record.
`POST /api/ai/turn`. `ConversationEntity` and re-keyed message tables. Five read tools and
`navigate_to`.

**Ends with:** the assistant answers questions about the project you are looking at and moves you
around the portal. No writes anywhere.
**Proves:** the client-driven loop, RBAC inheritance, and the cost model.

### Phase 1 — Proposals and the first pack *(no blockers)*

`ModalCatalog` and `open_modal`. The proposal card. Accept-dispatches-command in
`DecideAgentProposalHandler` — closing the gap that currently makes approval decorative. **The
Timesheet pack**, end to end.

**Ends with:** a worker enters a week of timesheets by chat, confirming each one.
**Proves:** the whole architecture on five fields and a tiny blast radius. If anything is wrong, it
surfaces here for the price of a small feature.

### Phase 2 — Orchestrator and QS *(blocked on the QS script)*

The capability-pack model, `switch_capability`, the Orchestrator. QS tools over variations, requests
and valuations. Contract-terms questions decline politely.

### Phase 3 — Autonomous runs *(blocked on §3)*

Worker DI expansion, first `TimerTrigger`, system pseudo-user and its capability set. Mailbox Triage
at 5 minutes — reviving `docs/triage-recommend-action-prompt.md`. Request Agent at 09:00.

**Do the infrastructure confirmations in §3 before writing agent code**, not after.

### Phase 4 — Bid Package *(blocked on §1 option A/B, §5, §6)*

Ship option A — scope from correspondence, not measurement. The Excel line schedule. Reply triage
and ranking. Anonymous submission decided separately, and probably later.

### Phase 5 — Programme *(blocked on §2 and §4)*

Wraps `SchedulingAgent` rather than replacing it. The 09:00 sweep produces drafts and proposals only.

---

## 9. The three questions to settle first

Everything above reduces to three decisions that are cheaper to make now than in six weeks.

1. **Does the QS script arrive intact, and how does it decompose?** It is the largest single input
   still missing, and the QS pack is the highest-value one. Expect it to split into pinned rules,
   an ordered method, and decision points that become `ask_user` calls.
2. **Is a contract entity in scope?** Two packs are specified to use contract terms and there is
   nowhere to read them. Deciding "not yet, and the assistant says so" is a perfectly good answer —
   deciding it *now* is what matters.
3. **What is the worker's hosting plan?** It determines whether autonomous agents are a
   straightforward timer job or a decomposition exercise across queue messages. One line of
   confirmation unblocks Phase 3's shape.
