# nigel-commercial-doctrine — Developer Brief & Handover Pack

**From:** Nigel Reilly, CEO — Jewel Bespoke Build Ltd / Jewel Enterprises
**To:** Development team, NR Consulting
**Date:** 10 August 2026
**Version:** v1.0
**Classification:** Internal — Commercial Doctrine

---

## SECTION A — EMAIL TO DEVELOPER

**Subject:** Handover pack — `nigel-commercial-doctrine` skill (v1.0) — portable Jewel commercial-control doctrine for AI models

Hi,

Attached is the full handover pack for a new AI skill I've built with Perplexity — `nigel-commercial-doctrine` — plus the underlying reasoning and reference material.

**What it is.** A portable, model-independent AI skill that encodes the commercial-control doctrine I use across every Jewel Bespoke Build commercial dispute, EOT/L&E claim, VO argument, and Contract Administrator response. Built on top of my existing `hold-ammunition-in-reserve` doctrine and the Jewel brand/naming skills, but packaged so it can be dropped into any AI model — Perplexity, Claude, ChatGPT, Gemini, a bespoke agent — and immediately give that model the ability to draft correspondence at chartered-QS level with the correct positional discipline.

**Why it exists.** Over the last several months we've built a body of commercial correspondence, reserve registers, contract-forensic summaries, and dispute doctrine on the 17A Abbot Road project. That work is directly applicable to every other Jewel commercial matter (By France, Nicholas Nymet, Albany Mews, Windy Ridge, Chiltern Court) and to future work. Without the skill, the reasoning lives only in individual sessions and gets lost when sessions compact or when we switch models. With the skill, the doctrine is durable and portable.

**What's in the pack.**

1. `nigel-commercial-doctrine.zip` — the packaged skill (main file + 5 references).
2. `SKILL.md` — the main file: five founding principles, eight-step working method, ten verified learnings, reserve register template, bootstrap sequence.
3. `references/jct-clause-map.md` — verified JCT clause numbering across ICD 2016, ICD 2024, SBC/Q 2016, SBC/Q 2024, MW 2016, DB 2016, DB 2024.
4. `references/rics-loss-and-expense.md` — RICS *Ascertaining Loss and Expense* 2nd edition (July 2024) heads of claim, methodology, evidence, concurrency, notify-or-else.
5. `references/case-law-anchors.md` — 12+ English construction cases with the specific propositions we rely on.
6. `references/portable-fact-pattern-library.md` — 8 recurring CA-side fact patterns with the correct move and the reserve card.
7. `references/session-continuity.md` — how a new model or session bootstraps back into the doctrine.

**What I need from you.**

1. Read the pack end-to-end. It's ~90 pages of markdown but every section earns its place.
2. Understand how the skill spec works — it follows the agentskills.io specification (used by Anthropic, Perplexity, and increasingly other AI platforms).
3. Get it working in whichever AI environments we deploy against beyond Perplexity — Claude Desktop, ChatGPT custom GPTs, bespoke agents.
4. Build the integration hooks so this doctrine can be triggered automatically by keywords in incoming emails from CAs (Contract Administrators) — the trigger phrases are listed in Section D.
5. Consider whether the reference material warrants being pulled into a structured database rather than markdown files, so we can query the JCT clause map, RICS heads, and case law anchors programmatically.
6. Advise on whether this should be released as a public template (with the project-specific material stripped out) as a marketing asset for NR Consulting — showing what commercial-grade AI skills look like in a real construction business.

**Timescale.** No rush. This is a durability play, not a fire-fight. But I'd like a first pass on the technical review within a fortnight.

**Confidentiality.** The pack contains commercial doctrine that is a competitive asset for Jewel. Do not share externally without approval. If we release a public version, it will be a sanitised template with the Jewel-specific fact pattern library removed.

Any questions, call me.

Nigel

---

## SECTION B — TECHNICAL SPEC

### B.1 Skill Format

The skill follows the **agentskills.io** open specification. Directory structure:

```
nigel-commercial-doctrine/
├── SKILL.md                                (main file, YAML frontmatter + markdown body)
└── references/
    ├── jct-clause-map.md
    ├── rics-loss-and-expense.md
    ├── case-law-anchors.md
    ├── portable-fact-pattern-library.md
    └── session-continuity.md
```

### B.2 Frontmatter

The SKILL.md file begins with a YAML frontmatter block declaring:

- `name` — kebab-case identifier, must match the directory name.
- `description` — a long-form description that includes the trigger phrases for auto-loading.
- `license` — MIT.
- `metadata` — author, version, scope, and companion skill list.

### B.3 Trigger Model

The skill is loaded when the AI detects any of the following signals in the user's input:

- Explicit request ("load nigel-commercial-doctrine", "use my commercial doctrine").
- Contract-side counterparty naming (Contract Administrator, CA, architect, Employer, principal contractor, subcontractor, insurer, loss adjuster).
- Contractual event phrases (EOT notice, L&E claim, variation dispute, pay-less notice, final account, retention release).
- Project name plus dispute context (17A Abbot Road, By France, Nicholas Nymet, Albany Mews, Windy Ridge, Chiltern Court — any of these plus "reply", "response", "letter", "draft").
- JCT clause references (JCT ICD, JCT SBC, clause 4.15, clause 4.17.4, clause 1.7).

### B.4 Companion Skill Dependencies

The skill is designed to be loaded alongside, and defer to, these existing user skills:

- `hold-ammunition-in-reserve` — the founding reserve doctrine.
- `jewel-variation-agent` — for VO/bid pack/tender/quote work.
- `jewel-brand-guidelines` — for Jewel-branded output.
- `jewel-naming-convention` — for correct entity naming.
- `clarify-intent-first` — upstream intent-clarification.
- `presentation-qa` — before sharing any drafted output.

If those companion skills are not present in the target environment, the doctrine still functions — but reserve-doctrine mechanics are richer when `hold-ammunition-in-reserve` is loaded.

### B.5 Cross-Platform Portability

The skill is written as pure markdown. It should function on any AI platform that supports:

- Instruction-following on 20+ page prompts;
- File-attachment or reference loading;
- Multi-turn conversation with state.

Verified working on Perplexity. Should work on Claude Desktop projects, ChatGPT with custom instructions + file uploads, and any agent framework that accepts an initial system prompt.

### B.6 Reference File Loading Pattern

The main SKILL.md is the always-loaded core. The five reference files are loaded on-demand:

- `jct-clause-map.md` loaded when quoting a JCT clause.
- `rics-loss-and-expense.md` loaded when drafting or defending an L&E claim.
- `case-law-anchors.md` loaded when a legal proposition is being advanced.
- `portable-fact-pattern-library.md` loaded when the fact pattern matches one of the eight recurring patterns.
- `session-continuity.md` loaded when bootstrapping a new session/project/model.

For environments that cannot load references on-demand, all files can be concatenated into a single prompt — total token count is manageable (~30,000 tokens for the full pack).

---

## SECTION C — DEPLOYMENT INSTRUCTIONS

### C.1 Perplexity (currently deployed)

Already live in Nigel's personal skill library. Loads automatically on Jewel commercial-correspondence triggers. Manageable at `https://www.perplexity.ai/computer/skills`.

### C.2 Claude Desktop (Anthropic)

1. Create a new Project in Claude Desktop.
2. Upload the six markdown files (SKILL.md + the five references) as Project Knowledge.
3. In the Project custom instructions, add: *"Load and apply the nigel-commercial-doctrine skill from the attached files on any Jewel commercial correspondence, EOT/L&E, VO dispute, or CA response task. Read the SKILL.md core; load reference files as the working method calls for them."*
4. Test with a sample CA-reply task.

### C.3 ChatGPT Custom GPT

1. Create a new Custom GPT via ChatGPT's GPT Builder.
2. Upload the six markdown files under Knowledge.
3. In Instructions, paste the contents of SKILL.md.
4. Set Actions off unless integrating with Outlook/SharePoint via API.
5. Test with a sample CA-reply task.

### C.4 Bespoke Agent (LangGraph / Anthropic API / OpenAI API)

1. Pass the SKILL.md content as the system prompt.
2. Load reference files into a retrieval store (Chroma, Pinecone, or similar) keyed to the file names.
3. Add a tool call for the agent to request a reference file by name when the working method calls for it.
4. Ensure the agent has file-write access for producing sent letters and paired reserve registers.

### C.5 Integration With Outlook

**Standing rule:** never auto-load drafts into Outlook without Nigel's explicit request. This applies on every deployment, in every environment.

If you build Outlook integration:

- Trigger inbound: parse emails from known CA addresses (Paul Rawsthorn, Alison Pressley, etc.) for trigger phrases.
- Trigger outbound: on Nigel's explicit "load this into my draft" command, and only then.
- Never auto-send. Human-in-the-loop is mandatory.

---

## SECTION D — TRIGGER PHRASES LIBRARY

The skill should auto-load on any of these phrases in the user's input:

**Contract & commercial events:**
- "draft a reply to [CA name]"
- "respond to Paul", "respond to Alison", "respond to [any CA]"
- "EOT notice", "L&E notice", "loss and expense", "loss & expense"
- "prolongation claim", "prolongation prelims"
- "variation rejected", "VO rejected", "VO dispute"
- "pay-less notice", "payment notice", "certification default"
- "final account dispute", "retention release"
- "final account negotiation"

**Contractual clause references:**
- "clause 1.7", "clause 4.15", "clause 4.17", "clause 4.17.4", "clause 2.20"
- "JCT ICD", "JCT SBC", "JCT MW", "JCT DB"
- "Relevant Matter", "Relevant Event", "impediment prevention or default"

**Project context:**
- "17A Abbot Road", "By France", "Nicholas Nymet", "Albany Mews"
- "Windy Ridge", "Chiltern Court", plus "reply / response / letter / draft"

**Explicit invocation:**
- "load nigel-commercial-doctrine"
- "apply my commercial doctrine"
- "use the reserve doctrine"

---

## SECTION E — FUTURE ENHANCEMENTS (BACKLOG)

Items to consider once the skill is deployed and tested:

### E.1 Structured JCT Clause Database

Convert `jct-clause-map.md` into a structured JSON or SQLite database. Query interface: `get_clause(edition, function)` returns clause number and verbatim wording. Advantage: eliminates the risk of the AI transcribing a clause number incorrectly.

### E.2 Case Law Auto-Update

Subscribe to construction law update feeds (Practical Law, Building Magazine, Construction Law Journal) and prompt for updates to `case-law-anchors.md` when a new decision on notice-as-condition-precedent, concurrent delay, or JCT interpretation is handed down.

### E.3 Reserve Register Auto-Generation

Build a small tool that takes a sent letter as input and produces the paired reserve register template automatically, prompting Nigel for the reserve card list.

### E.4 Fact Pattern Auto-Detection

Train a classifier on incoming CA emails to auto-detect which of the eight fact patterns is in play and pre-load the matching playbook.

### E.5 Public Marketing Version

Sanitise the pack (remove Jewel-specific fact patterns, replace with generic examples) and release as a public template on NR Consulting's website. Positioning: *"Commercial-grade AI skills for construction contractors — the reasoning, not just the boilerplate."*

### E.6 Cross-Project Ammunition Sharing

Build a mechanism for reserve registers from one project to inform another — e.g. if a CA on Project A has misstated a clause, the same CA on Project B is likely to make the same mistake. Currently manual; could be automated.

---

## SECTION F — SECURITY & CONFIDENTIALITY

- Do-not-disclose items are hard-coded into the doctrine: sub costs, procurement pricing, MGN/trade-package rates, internal margin, internal correspondence with legal/insurance advisers.
- If any AI deployment produces output containing these items, that is a serious defect and must be treated as an incident.
- The skill itself is a commercial asset. Distribution outside NR Consulting / Jewel Enterprises requires Nigel's explicit approval.
- The reference material in `case-law-anchors.md` is public but the way it is combined with the fact pattern library is proprietary methodology.

---

## SECTION G — ATTACHED FILES

1. `nigel-commercial-doctrine.zip` — the packaged skill.
2. `SKILL.md` — main file.
3. `jct-clause-map.md` — reference 1.
4. `rics-loss-and-expense.md` — reference 2.
5. `case-law-anchors.md` — reference 3.
6. `portable-fact-pattern-library.md` — reference 4.
7. `session-continuity.md` — reference 5.

Total pack size: ~65KB zipped, ~90 pages of markdown.

---

**End of brief.**
