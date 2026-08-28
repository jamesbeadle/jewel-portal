> **Superseded (2026-08-27).** The in-portal chat this document describes was retired in favour of the MCP connector — see [10-mcp-connector.md](10-mcp-connector.md). Kept as the historical record.

# The QS Capability Pack

> Decomposition of *QS & Commercial Controller — Portable Skill Pack v1.0* into the pack model from
> `00-agent-architecture.md` §6.
>
> The pack is written for a general-purpose chat model with no system behind it. Ours has a portal,
> a database and a contract row. Roughly a third of the pack should therefore **not** be prompt at
> all — it should be code, tools, or a template. This document is that split.

---

## 0. The split, in one table

| Skill-pack section | Becomes | Why |
|---|---|---|
| Part 1 — identity, objectives, operating principles, conversation behaviour | **System prompt fragment** | This is voice and judgement. Prompt is the right home. |
| Part 1 — "You must never" / "You must always" | **Pinned context** (Layer 2) | Must never be forgotten mid-conversation. Non-negotiable. |
| §2.1 Contract forms | **Tool** — `get_project_contract` | We know the form. Don't make the model guess from a table. |
| §2.2 Daily clauses | **Pinned, conditionally** | Only the clause set for *this project's* form, and only when `IsAmended` is false. |
| §2.3 Golden rules of variation management | **Pinned** | The rules that lose money when forgotten. |
| §2.4 Valuation rule hierarchy | **Pinned** | Ordered, short, applied constantly. |
| §2.5 OH&P standing rules | **Tool** — `get_project_contract` | Now per-project contract terms, not general knowledge. |
| §2.6 Interim application discipline | **Code + tool** | Dates are arithmetic. See §5. |
| §2.7 CVR discipline | **Tool** + prompt | The figures come from tools; the commentary is the model's. |
| §2.8 Subcontractor management | **Prompt fragment** | Judgement. |
| §2.9 Final accounts | **Prompt fragment** | Judgement, plus `SettlementRecordEntity`. |
| §2.10 Risk register | **New entity needed** | No table exists. See §6. |
| Part 3 — written output templates | **Deterministic scaffolds** | See §4. Do not trust a model to remember a legal paragraph. |
| Part 4 — commercial playbook | **Pinned** (4.1) + **new entity** (4.2 precedents) | See §6. |
| §4.3 Client behaviour patterns | **Prompt fragment** | Pattern recognition — exactly what a model is good at. |
| Part 5 — operational loops | **Scheduled agents**, not chat | Daily/weekly/monthly loops are the worker's job. |
| Part 6 — deployment guide | **The contract upload flow** | Already built: `ProjectContracts`. |
| Part 7 — anti-patterns | **Pinned** | Short, absolute, high-consequence. |
| Part 8 — quick-reference card | **Completion policy** | This is the pack's definition of "done". |

---

## 1. Pinned context — the non-negotiables

Layer 2 (architecture §7). Loaded on every turn the QS pack is in force, ~350 tokens. These are the
rules where being wrong costs money or credibility, so they never scroll out of the window.

**Never:**

- Guess a clause number. Verify it against the project's contract or say plainly that you are
  working from principle and it needs checking.
- Recommend "remeasure at final account" for a client-side scale-off dispute unless the item was
  expressly issued remeasurable. One concession makes every priced variation negotiable.
- Disclose contractor supplier invoices on a lump-sum variation. The client pays the priced rate;
  procurement risk and reward are the contractor's.
- Concede a variation position without documenting why and flagging the precedent implication.
- State a figure that did not come from a tool result.

**Always:**

- Apply the valuation rules in order: existing rates → similar rates pro-rata → fair rates →
  daywork → direct loss and/or expense.
- Treat OH&P as a contract term read from `get_project_contract`, never as a remembered default.
- No AI, no variation. Varied work needs a written instruction before commencement; if site pressure
  forces a start, a CVI or EWN goes out the same day.
- Every variation letter carries a reservation-of-rights paragraph. This is appended by the system,
  not by you — do not write your own and do not omit it.
- Log every commercial event with date, source document, decision and clause reference.

**Anti-patterns** (Part 7, compressed): don't concede open remeasure; don't disclose supplier
invoices; don't miss a notice deadline; don't argue a losing position; don't price without citing
the valuation rule; don't submit a CVR without recalculating; don't agree anything commercial
verbally; don't let a dispute drift.

**Conditional block.** When `ProjectContract.IsAmended` is false, pin the daily clause table for the
project's form (§2.2). When it is true, pin `BespokeDeviations` instead, with the instruction: *this
form is amended — cite from the deviations, or say the clause needs checking.*

---

## 2. System prompt fragment — voice and judgement

Everything in Part 1 that is *character* rather than *rule*, with the placeholders resolved from the
contract row rather than at deployment: `{CONTRACTOR}` from `ContractorName`, `{EMPLOYER}` from
`EmployerName`, `{CA}` from `ContractAdministratorName`, `{CONTRACT_FORM}` from `FormDisplayName`.

Two amendments to the pack's wording, both because the pack assumes no system underneath:

**"Maintain a live variation register, risk register and CVR position."** The system maintains
these. The pack's instruction becomes: *read them before answering, and propose updates — never
narrate a register you are holding in your head.*

**"Flag every notice deadline before it lapses."** A model cannot be relied on to notice a date
passing between conversations. This moves to the scheduled sweep (§5). The prompt keeps only:
*when a notice deadline is within seven days, say so unprompted.*

Keep verbatim, because they are unusually well-judged: the "be honest / concede cleanly" principle,
"commercially astute, not adversarial", and "lead with the commercial position, then the reasoning,
then the clause references". That last one is a genuinely good output shape and should be enforced
by example in the fragment.

---

## 3. Tools

`get_project_contract` is the pack's most important tool and should be called before any clause
citation, any OH&P figure, any retention percentage and any notice-period arithmetic.

| Tool | Kind | Backed by | Status |
|---|---|---|---|
| `get_project_contract` | R | `GetProjectContract` | **Built** |
| `get_request_context` | R | `RequestContextAssembler` | Built, needs wrapping |
| `list_variations_for_project` | R | `ListVariationOrdersForProject` | Built |
| `get_variation` | R | existing query | Built |
| `list_requests_for_project` | R | existing query | Built |
| `get_valuation_position` | R | claim totals, certified to date, retention | Built |
| `list_cost_centres` | R | existing query | Built |
| `get_cvr_position` | R | `CvrSnapshotEntity` | Built |
| `list_project_precedents` | R | — | **New entity needed** (§6) |
| `list_project_risks` | R | — | **New entity needed** (§6) |
| `list_notice_deadlines` | R | computed from the contract | **New, code** (§5) |
| `create_variation_from_request` | W | `CreateVoqFromRfq` | Built |
| `set_variation_status` | W | `SetVariationOrderStatus` | Built — Quoting ⇄ Issued ⇄ Awaiting AI only |
| `draft_request_email` | W | `PrepareRequestEmailDraft` | Built |
| `draft_request_reply` | W | `PrepareRequestReplyDraft` | Built |
| `record_project_precedent` | W | — | **New** (§6) |
| `raise_project_risk` | W | — | **New** (§6) |

**Not tools, deliberately:** `approve_variation` and `reject_variation`. Approval writes valuation
lines, a QS accrual and cost-centre budgets in one transaction. It is a commercial commitment and
belongs to a person on the variation page. The assistant may navigate them there and summarise what
approval will do.

---

## 4. Written outputs are scaffolds, not memory

Part 3 gives seven letter patterns, each ending in a reservation-of-rights paragraph, and Part 4.1
rule 8 calls that paragraph **non-negotiable**.

A model will produce it correctly forty-nine times in fifty. On a system that drafts contractual
correspondence, one in fifty is not an acceptable rate for omitting a rights reservation.

**So the system appends it, from the contract row.** A `ReservationOfRights` renderer takes the
project's `ContractForm` and the clause set in play and emits the paragraph with the right clause
numbers. It is applied to every outbound draft from the QS pack, after the model has finished. The
model is told this in the pinned block so it does not write a second one.

The seven patterns themselves become **named draft shapes** the model selects between, each a
three-part skeleton it fills:

| Shape | Used when | Structure |
|---|---|---|
| `variation_price_challenged` | Client or CA trying to reduce a priced variation | Price is fixed unless issued remeasurable → scope covered → valuation rule cited → invite an omission AI if they want a reduction |
| `variation_concession` | We are genuinely in the wrong | Concede the principle plainly → ring-fence what we absorb → ring-fence what remains chargeable → revised variation |
| `instruction_chase` | Work directed verbally or by email with no AI | Acknowledge → cannot commence without a written AI → what the AI must contain → programme risk |
| `eot_notice` | Relevant event | Notice with event, date and cause → nature and extent → particulars to follow → L&E reserved |
| `payment_notice_response` | Client misses or botches a notice | Applied sum is the notified sum → payable in full on the final date |
| `free_replacement` | Client challenging cost of a replaced damaged item | Concede the free replacement → ring-fence spec uplift and remedial cost → less supplier credit |
| `scope_add_chaser` | Client assuming something not in contract | Not in scope, here's what I checked → offer as a variation → how it will be priced → invite them to point at anything missed |

Selecting the shape is the model's judgement. Filling it is the model's writing. The rights
paragraph and the signature block are the system's.

---

## 5. The loops belong to the scheduler

Part 5's daily, weekly and monthly loops are not chat behaviour. They are the Request Sweep and a
new commercial sweep, running on the timer infrastructure (`02-gaps-and-roadmap.md` §3).

The contract row makes the most valuable one arithmetic rather than judgement:

```
application_date        = ApplicationCutOffDayOfMonth, each month
payment_notice_due      = application_date + PaymentNoticeDays
final_date_for_payment  = due_date + FinalDateForPaymentDays
pay_less_notice_due     = final_date_for_payment − PayLessNoticeDays
```

Compute these in code, not in the model. `list_notice_deadlines` returns them with days remaining;
the sweep raises anything inside seven days. Part 4.1 rule 10 — *set calendar alerts one week and
one day before every notice deadline* — becomes a scheduled query, which is the only way it will
actually happen.

The pack's rule that a late Payment Notice makes the applied sum payable in full is then a
detectable event, not something someone has to remember to check.

---

## 6. Two things the pack needs that the system does not have

### Risk register (§2.10)

No table exists. `docs/05-data-model/entities.md` lists one; nothing implements it. The pack calls
for a live register per project, updated monthly with the CVR: id, description, category,
likelihood 1–5, impact £, owner, mitigation, status.

Small entity, high value — and it is what makes "top 3 risks with £ exposure" in the CVR a query
rather than a paragraph the model invents each month.

### Precedent log (§4.2) — the more interesting one

This has no equivalent anywhere in the system, and it is arguably the single most valuable thing the
skill pack adds.

> *Once a precedent is set on a project, it applies to every subsequent variation on that project.*

An assistant that does not know what was conceded on V12 will contradict it on V31, and the client
will notice. A precedent record — project, date, subject (OH&P treatment, remeasure position,
daywork rates, application format), the position taken, the variation or letter that set it, and who
set it — turns "be consistent" from a hope into a lookup.

It is also the pack's handover checklist (§6.3) made real: *standing precedents on the project* is
listed as something that must transfer between agent instances. In a system, it transfers by being
a row.

**Suggested shape**, following house conventions (string PK, no FKs, enums as int):

```
ProjectPrecedentEntity
  ProjectPrecedentId (PK)   ProjectId
  Subject (int enum)        — OhpTreatment | RemeasurePosition | DayworkRates | ApplicationFormat | NoticeProcess | Other
  Position (nvarchar 2000)  — what was established, in plain English
  EstablishedOn             EstablishedByEmail
  SourceRecordType (int?)   SourceRecordId (string?)   — the variation or request that set it
  SupersededAt (DateTimeOffset?)  SupersededReason (nvarchar 1000?)
```

Precedents are superseded, never edited — the history is the point.

---

## 7. Clarify Intent First

The pack's "Clarify Intent First" pattern maps directly onto the `ask_user` UI tool, and it should
be a **tool call, not a paragraph of questions**. The chat panel renders tappable options; the user
answers in one tap rather than composing a reply.

The pack's own carve-out is the important half and must survive into the prompt: *if the question is
a simple factual lookup or a direct command with all parameters specified, skip the clarify step and
answer directly.* Without that, an assistant that asks four questions before telling you the value
of V72 will be switched off within a week.

Trigger it only when a wrong assumption would materially damage margin — the pack's own test.
Concretely: which valuation rule applies, whether an item was issued remeasurable, whether to
concede a position, and who a letter is addressed to. Not: what a figure is, what a status is, or
what happens next in a workflow.

---

## 8. Completion policy

Part 8 is the pack's definition of done, and it becomes the QS pack's `CompletionPolicy`:

- Every variation has a written instruction, a rights reservation, an explicit OH&P treatment, a
  status, and a register entry dated the day it arose.
- Every notice was served in writing inside the contract period, to the addressee in the Contract
  Particulars, with particulars to follow.
- Every application went out on the cut-off date with full substantiation, variations shown
  separately with status, and diary entries for the notice dates.
- Every CVR reconciles to source, forecasts cost-to-complete by trade, carries movement commentary
  and names the top three risks with exposure.

The first three are checkable by query once the precedent and risk entities exist. The fourth is
checkable except for the commentary. That is a good ratio: the machine checks the discipline, the
human judges the commentary.

---

## 9. Open questions for Nigel

1. **OH&P defaults.** The pack gives 10% direct, nil omission, 5% attendance, 15/10/10 daywork.
   Should new projects seed with these, or start blank so nobody inherits a rate that was never
   agreed? Blank is safer; seeded is faster. The entity supports either.
2. **Precedent capture.** Should recording a precedent be a proposal the QS confirms, or should the
   assistant record one automatically whenever it detects a position being established in
   correspondence? Automatic is more complete and more wrong.
3. **Which clause set is authoritative.** §2.2 is written against JCT MWD 2016. The programme agent's
   existing code cites JCT ICD 2024 (`SchedulingAgent`). Which forms are actually in use across live
   projects — that determines how many clause tables need pinning.
4. **Risk register ownership.** The pack assigns each risk a named individual. To-dos in this
   system are assigned to a **role** first, optionally pinned to a named holder of it
   (`TodoItemEntity.AssigneeRole` + `AssigneePersonEmail`) — the pin falls back to the role if the
   person leaves. Risks could follow the same role-plus-optional-person convention.
