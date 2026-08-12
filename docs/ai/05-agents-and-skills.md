# Agents and Skills — the knowledge layer, plainly

> Companion to `04-orchestration.md` (the machinery) and a response to two inputs that arrived
> together: the backlog epic **7 — Create AI Orchestrator** (7.1 Add Nigel Skills · 7.2 JBB Second
> Brain · 7.3 Bid Packages · 7.4 Timesheets · 7.5 Programme · 7.6 Contracts Builder · 7.7 Materials
> Buyer · 7.8 Chaser) and Nigel's **commercial doctrine pack** (email 2026-08-10, "COMMERCIAL HEAD
> BRAIN WITH QS": `nigel-commercial-doctrine`, `commercial-director`, mistake-prevention, plus five
> reference files, in agentskills.io format).
>
> The first half of this document is written to be readable by anyone on the team, Nigel included.
> The second half is the design. Decisions taken 2026-08-12: skills are **DB-backed and managed in
> the portal**; Nigel's pack is ingested **verbatim** (decomposed later, with him); this document
> and the refreshed map are the record.

---

## 1. The model in plain English

There are exactly three kinds of thing. Everything in the epic is one of them.

**The assistant** is the chat panel. There is one of it, one conversation at a time, and it is the
only thing that ever talks to the user. It already works: it can read the portal, answer questions
about the project on screen, move you around the site, and fill in a registered form beside the
chat. It never submits anything — every form is pressed by a human, every email leaves as an
Outlook draft a human sends. That is not a temporary limitation; it is the design, and it is also
D7 of Nigel's own doctrine ("never auto-send") and D8 ("the user is the last line of defence").

**An agent is a hat the assistant puts on.** It is not a second chatbot and the user never gets
"transferred" to it. When the conversation turns to pricing a variation, the assistant switches
into the commercial hat: its rules of engagement change, its tool set changes, and — this is the
new part — **its knowledge changes**. Same conversation, same thread, different discipline in
force. In the code a hat is a *capability pack*: a named bundle of (a) which tools it may use,
(b) which forms it may open, (c) which **skills** it has read. The backlog's "Bid Packages Agent",
"Timesheets Agent", "Programme Agent" are each one pack.

**A skill is a training manual an agent has read.** It is a markdown document — five founding
principles, an eight-step method, a table of standing rates, a list of past mistakes and how to
avoid them — written by the person who owns that discipline, not by a developer. Nigel's
`commercial-director` skill is precisely this, and it is the model for all of them. Skills live in
the portal, are versioned, and are attached to agents. **This is where the "complex business
decisions" live.** The code gives an agent hands; the skills give it judgement.

And the **orchestrator** is simply the hat the assistant wears when no discipline is engaged:
front of house. It answers the cheap questions itself, navigates, opens forms — and its most
important skill is recognising *whose job something is* and switching hats **before any content is
drafted**. That last clause is the rule this document exists to state:

> **Content is never drafted from the orchestrator.** The moment a turn involves producing words,
> figures or line items that will end up in a form, a letter or a record, the correct agent's pack
> must be in force first — its doctrine loaded, its tools available, its Never-rules pinned. The
> orchestrator routes; agents draft; the user submits.

### One worked example, end to end

*"Draft a reply to Paul — he's rejected V72."*

1. **Orchestrator** recognises the triggers ("draft a reply to [CA]", "VO rejected" — straight out
   of Nigel's trigger-phrase library, which becomes routing data) and calls
   `switch_capability("commercial", reason)`. One clause to the user: *"That's a commercial
   reply — switching to the commercial agent."*
2. **Commercial agent** is now in force. Its pack pins the doctrine's non-negotiables and its
   skills are loaded: hold ammunition in reserve, reservation of rights on every reply, never
   disclose sub costs, verify every clause.
3. It **verifies before it cites**: `get_project_contract` (built) returns the form, edition,
   amendments and `isAmended` — doctrine D4 enforced by a tool, not by memory.
4. It **reads the actual correspondence**: `get_request_context` pulls the thread on the variation's
   request — evidence inventory, doctrine step 3.
5. It **drafts on the minimum** (doctrine steps 4–5) and the draft lands as things a human
   controls: the reply as an Outlook **draft** (never sent by any agent — structural, ADR-006),
   and a **reserve register entry** as a proposal card the user confirms (doctrine D1/D6, see §5).
6. **The user** reads, edits, presses the buttons. Their press is the check — same as the
   variation form, same as every dialog.

Nothing in that flow needed a second AI, a hand-off, or an "agent talking to an agent". It needed
the right knowledge in force at step 2 — which is the whole argument.

---

## 2. Skills: the design

### 2.1 Format — adopt agentskills.io as-is

Nigel's pack already follows it and it is the right shape: YAML frontmatter (`name`,
`description`, metadata) + a markdown body + a `references/` folder of larger documents loaded on
demand. The `description` field matters most: it is what the **orchestrator** reads when deciding
which agent a job belongs to, and what an **agent** reads when deciding whether to pull a
specialist skill mid-task. Nigel's trigger-phrase library (his DEV-BRIEF §D) folds into the
descriptions and a `triggers:` metadata list.

### 2.2 Storage — in the portal, versioned, admin-managed

```
SkillEntity
  SkillKey (unique)        DisplayName        Description (frontmatter, verbatim)
  Body (markdown, unbounded)                  TriggersJson
  Version (int)            IsActive           UpdatedByEmail   UpdatedAt

SkillReferenceEntity
  SkillKey  RefKey  DisplayName  Description  Body (markdown)  Version
```

Plus a **Skills admin page** in JPMS (role-gated: Admin/MD): upload a `.zip` or `.md` in
agentskills.io format, see the parsed frontmatter, activate/deactivate, and a version history —
each save is a new version, old versions kept. Nigel authors in Perplexity or wherever he likes
and uploads the result; **no code deploy moves doctrine**. Every hop's activity row already
records the pack in force; add the skill versions in force, so "what did the assistant know when
it drafted this" is answerable years later — same dispute-first logic as the conversation record.

Skills are commercial assets (his §F). They live in the DB behind the existing auth, are never
served to non-internal roles, and are never quoted wholesale to a user — they shape drafting.

### 2.3 Loading — pinned master, lazy everything else

This follows ADR-005 (layered and lazy) exactly, and it also happens to be how Nigel's own load
order works ("load the specialist when the task lands — do not freelance"):

| Layer | What | When |
|---|---|---|
| Pack pinned | The agent's **master skill body** (e.g. `commercial-director`: doctrines, standing rules, decision framework) | In the system prompt whenever the pack is in force |
| Named, not loaded | The pack's **specialist skills and references**, listed as one line each (name + description) | The model sees what exists |
| `load_skill(key)` | A specialist's full body (e.g. `variation-authoring`, mistake-prevention) | Read tool, on demand, mid-conversation |
| `load_skill_reference(key, ref)` | A reference file (e.g. `jct-clause-map.md`, `case-law-anchors.md`) | Read tool, on demand |

Two rules keep this affordable. Specialist bodies and references replay **latest-only** in the
transcript (the same supersede-stubbing `get_request_context` needs — one mechanism, §6 of the 04
doc). And a master skill body has a size budget; if it grows past it, the overflow belongs in a
specialist. Mistake-prevention (`M1–M18`) is the interesting case: its own frontmatter says "must
be read fully" — it loads with the pack whenever the pack can *write into a form or draft*, and is
skippable for pure Q&A turns.

### 2.4 Composition — packs point at skills

`CapabilityPack` (04 §2.2) gains one list:

```csharp
IReadOnlyList<string> SkillKeys   // master(s) pinned; their specialists/references become loadable
```

So the full definition of an agent is now honest: **tools it may use + forms it may open + skills
it has read + who may engage it + what "done" means.** Renaming stays cheap; the roster stays a
registry.

One consequence worth stating for the orchestrator: its routing knowledge is **derived** — the
pack list with each pack's skill descriptions and triggers, assembled per turn. Nobody hand-writes
"when to use the commercial agent" prose; Nigel's own description field is that prose.

---

## 3. Nigel's pack, mapped

Ingested verbatim (decision above), attached to the **commercial agent** (working key
`commercial` — covers the epic's QS/variations/delay ground; naming is fine-tunable, as ever).
He flagged two generations in the pack (the By France and Abbot builds — `commercial-director`
vs `nigel-commercial-doctrine`); ingest **both as separate skills**, mark the newer active on the
pack, and reconcile with him once it has been used in anger. The five references ingest as
references of the doctrine skill.

What is striking — and worth telling him — is how much of his doctrine the platform already
enforces *structurally*, which is stronger than a prompt promise:

| His doctrine | Enforced by |
|---|---|
| D7 Never auto-send | ADR-006: no agent tool can reach `SendDraftAsync`, by wiring. Drafts only. |
| D8 QA before share / user is last line | The dialog-as-proposal pattern: the user presses every button. |
| D4 Verify every clause | `get_project_contract` (built): form, edition, amendments, `isAmended` gates clause citation. Already pinned in the system prompt's Never rules. His backlog E.1 (structured clause DB) is this tool. |
| D2 Reservation of rights | Drafting rule — stays in the skill, where he can tune the wording. |
| D3 Never disclose sub costs / margin | Three layers: pinned Never rule; client-safe rendering is a *tool* property (a client-facing draft tool renders contract-basis rates + OH&P, never net cost — code, not judgement); and the human gate. See §5. |
| D1/D6 Reserve register + reasoning note | New record: see §5. This is the one real build item his doctrine adds. |
| D5 CA-as-QS conflict | `ProjectContractEntity` already names Employer, CA, Architect, QS — the conflict is *derivable*; surface it in `get_project_contract`'s result. |
| Intake (Business/Project Profile) | Already exists as data: the JBB Second Brain skill (business profile) + `get_project_contract` and the project record (project profile). His `commercial-director-intake` becomes unnecessary inside JPMS — the profile is read, not interviewed. |
| Trigger phrases (§D) | Pack/skill `triggers` metadata → orchestrator routing. |
| E.4 Fact-pattern auto-detection | The mailbox-triage worker run, later — a proposal ("this looks like fact pattern 3"), never an action. |

---

## 4. The epic, mapped (tasks 7.x)

| Task | What it actually is | Depends on |
|---|---|---|
| **7 Create AI Orchestrator** | 04's build-order step 2 (pack model + `switch_capability` + route-based initial pack) **plus** the routing knowledge from skill descriptions (§2.4). | Transcript fix (04 §6) first |
| **7.1 Add Nigel Skills** | The skill store (§2.2), the two `load_skill*` tools (§2.3), the admin page, and verbatim ingestion of the pack (§3). | Nothing — buildable now |
| **7.2 JBB Second Brain** | A **skill, not an agent**: the company-wide profile — house language, record lineage, operating model, naming rules — lifted from `CLAUDE.md`/glossary into a managed skill pinned into the *shared* preamble for every pack. What is hardcoded in `AiSystemPrompt` today becomes v1 of this skill. | 7.1 |
| **7.3 Bid Packages Agent** | Pack: tools per `01-agent-specifications.md` §3 + skills (`subcontractor-quote-vetting`, tender fact patterns). Scope-from-correspondence now; Bluebeam later (gaps §1, option A). | 7, 7.1 |
| **7.4 Timesheets Agent** | Pack: the smallest — `submit_timesheet` dialog + worker-profile read tools. Best end-to-end proof of dialog + pack + skill together. | 7 |
| **7.5 Programme Agent** | Pack: wraps `SchedulingAgent` as a read tool; NOD/EOT via the raise-request dialog; `delay-analysis` + doctrine skills carry the JCT judgement. | 7, EOT linkage (gaps §4) |
| **7.6 Contracts Builder** | Pack over the contract slice (ADR-008, built): record/amend contract dialogs, `tender-review-precontract` skill, terms Q&A via `get_project_contract`. | 7, 7.1 |
| **7.7 Materials Buyer Agent** | Pack — **needs scoping first**: `search_local_suppliers` exists and `WorkOrderEntity` is the purchase order, but there is no materials/pricing data model behind it. Write its spec before building (same treatment as `01` gave the others). | 7, a scoping session |
| **7.8 Chaser Agent** | The **autonomous** one: the 09:00 request sweep (worker, timer, pseudo-user, drafts + proposals only) from 04 §2.5 — with the doctrine nuance that chasing is *posture*, per the skill ("set posture, not chase"). | Worker infra (gaps §3) |

Revised build order, folding this into 04 §5: **(1)** transcript fix → **(2)** pack model =
task 7 → **(2a)** skill store + loaders + ingestion = tasks 7.1/7.2 → **(3)** proposal write path
(now also carries the reserve register) → **(4)** `ask_user`/`highlight` → **(5)** dialogs for the
packs in play → **(6)** worker runtime = task 7.8 → **(7)** widen the roster = 7.3–7.7 as their
blockers clear.

---

## 5. Two build items the doctrine adds

**The reserve register.** D1/D6 want every outbound commercial letter paired with an internal note:
position taken, evidence deployed, evidence held back, escalation path. In JPMS terms this is a
small record (`ReserveNoteEntity`: project, request/variation link, the four fields, author,
linked draft's Graph id) plus a registered dialog so the agent drafts it and the human confirms it
— the same shape as everything else. It is internal-only by role. This also answers his backlog
E.3 (auto-generate the register from a sent letter) the JPMS way: the agent drafts both together,
in one conversation, and the audit trail links them.

**Client-safe rendering as a tool property.** D3's "duplicate and strip" is exactly the class of
rule that must not depend on the model remembering it. Any tool that produces a client-facing
document from cost data declares — in code — which fields it renders (contract-basis rates, OH&P)
and which it structurally cannot emit (net cost, sub rates, margin). A leak then requires a code
change, not a bad day. Flag any violation as an incident, per his §F.

---

## 6. What to tell the wider team

The epic's names survive; the things they name are now precise. The orchestrator is a pack whose
skill is routing. An agent is a pack: tools + forms + skills. A skill is a versioned markdown
manual, managed in the portal by the person who owns the discipline. Content is drafted only with
the right pack in force; forms are submitted only by people; email leaves only as drafts; and the
knowledge that makes any of it commercially competent is Nigel's to write — the platform's job is
to make sure it is *in force at the right moment* and *impossible to violate structurally* where
that can be had.
