# Session Continuity — Bootstrapping the Doctrine

**Purpose.** Instructions for reactivating this doctrine in a new session, new project, or on a new model. This file is the answer to the question: *"how do I continue this work when I open a new conversation?"*

## When To Read This File

Read this file when:

- Starting a new Perplexity session for a Jewel commercial matter;
- Beginning work on a new Jewel project;
- The user says "insert this into a new project" or "continue this in a new session";
- A different AI model has been switched to mid-work;
- Session context has been compacted and clean-slate work is beginning again.

## The Bootstrap Sequence

Follow these six steps in order. Do not skip any of them.

### Step 1 — Load the doctrine

Load this skill (`nigel-commercial-doctrine`) with `scope="user"`. Confirm the five founding principles are understood:

1. Answer only what was asked.
2. Play the second-strongest evidence; hold the strongest.
3. Never disclose material the counter-party does not know you hold.
4. Use their own record against them first.
5. Log every reserve card in a paired internal file.

### Step 2 — Load the companion skills

Load in this order:

- `hold-ammunition-in-reserve` (`scope="user"`) — the founding doctrine.
- `clarify-intent-first` (`scope="user"`) — upstream check.
- `jewel-variation-agent` (`scope="user"`) — if the matter involves a VO / bid pack / tender / quote.
- `jewel-brand-guidelines` (`scope="user"`) — for any deliverable that will bear Jewel branding.
- `jewel-naming-convention` (`scope="user"`) — always.
- `presentation-qa` (`scope="user"`) — before sharing any drafted output.

### Step 3 — Establish the project context

For each new Jewel project, confirm:

- **Project name and address** — e.g. "17A Abbot Road, Guildford".
- **Jewel entity** — usually Jewel Bespoke Build Ltd, but confirm.
- **Contract form and edition** — read the executed contract PDF if not already summarised. Produce a contract-forensic summary if none exists.
- **Article 4 CA and Article 5 QS** — named firms. Flag if same firm.
- **Contract Sum, Date for Completion, LADs, Rectification Period, Retention** — from Contract Particulars.
- **Insurance Option** — A / B / C.
- **CDP scope** — items and PI cover.
- **Amendments and struck-through provisions** — anything bespoke.

The output of this step is a `contract_forensic_summary.md` file in the project workspace. If one exists, read it and confirm it is current. If not, produce one.

### Step 4 — Establish the commercial state

For the specific matter being worked on, establish:

- **Counter-party** — CA / architect / QS / Employer / subcontractor / insurer.
- **Copy list** — who is watching this correspondence. This drives whether the architect-PI route can be deployed at all.
- **The current challenge** — what has the counter-party said. Read their letter verbatim.
- **The commercial position** — what does Jewel need to achieve on this reply (positional close, entitlement anchor, cash release, escalation setup).

Never draft a reply without first asking Nigel — via `ask_user_question` — what he wants to achieve.

### Step 5 — Take evidence inventory

Before drafting anything, list every piece of evidence Jewel holds that bears on the challenge. Structure the list as three columns: evidence / source side / weight.

### Step 6 — Apply the working method

Follow the eight-step working method from the main SKILL.md:

1. Verify the contract before you cite a clause.
2. Cross-check the counter-party's assertions against the actual contract text.
3. Take evidence inventory.
4. Match evidence to the minimum needed.
5. Draft the reply on that minimum.
6. Build the paired reserve register.
7. Do NOT auto-load into Outlook.
8. Set posture, not chase.

## Standing Rules — Always Active

These rules apply on every session, every project, every model:

- **Never auto-load into Outlook.** Every draft is shared with Nigel for review first. Only load into Outlook when explicitly requested.
- **Never disclose sub costs, procurement pricing, or margin data to any client-side party.** These are permanent do-not-disclose items.
- **Never use "Jewel Group".** Always "Jewel Enterprises" for the parent entity. Specific subsidiary names (Jewel Bespoke Build Ltd, etc.) are left exactly as they are.
- **UK English.** Colour, organise, recognise, behaviour.
- **XLSX not PDF for VOs.** The house standard.
- **SharePoint URLs use the `files` connector.** Never `browser_task` for SharePoint.
- **Cost codes from JBB Cost Code Master v2.1** — for every VO, bid pack, tender, or quote.

## What To Share With A New Model

If a new model is being onboarded to this work (e.g. Nigel is switching from one model to another), share:

1. This skill file (`nigel-commercial-doctrine/SKILL.md`).
2. The four reference files in the `references/` folder.
3. The companion user skills listed in Step 2.
4. The relevant project's `contract_forensic_summary.md`.
5. The relevant project's Brain wiki entry.
6. The last three or four sent letters and paired reserve registers on the current matter.

That is enough for a new model to pick up the doctrine and continue the work without loss of continuity.

## How To Recognise That The Doctrine Is Being Applied

Signs it is working:

- Draft letters are short, focused, and answer only what was asked.
- Every quoted clause is verified against the executed contract.
- Every sent letter has a paired reserve register in the workspace.
- Draft letters are shared with Nigel before Outlook loading, every time.
- Sub costs, procurement pricing, and internal Jewel material never appear in outgoing correspondence.
- Kill-cards are held until the counter-party attacks Jewel's numbers or competence.

Signs it is not working:

- Draft letters are long, wide-ranging, and volunteer arguments the counter-party did not raise.
- Clause numbers are cited from memory or from prior edition assumptions.
- Sent letters have no paired reserve register.
- Draft letters have been loaded into Outlook without explicit request.
- Internal Jewel material has been included in client-side correspondence.
- Kill-cards have been paraded rather than held.

If any of the "not working" signs appear, stop the current draft and re-load this skill.

## Version and Update Policy

- This skill is `v1.0` as of first-load date.
- Updates come from: new fact patterns identified on live projects, new case law developments, new RICS guidance editions, changes to Nigel's operating rules.
- When updating, bump the version number in the SKILL.md frontmatter and note the change in a `CHANGELOG.md` at the skill root.
- The doctrine itself is stable — the founding five principles and the eight-step working method are not up for revision without deliberate cause.
