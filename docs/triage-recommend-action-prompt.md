# Triage "Recommend action" — rough Claude prompt

Status: **Retired 2026-07-22.** The button and its full implementation (handler, contract,
endpoint, UI) were removed pending the pathway-first triage redesign
(`docs/Pathway-Split-Platform-Flow-Plan.md`). This doc is kept as the spec to revive from.

Feature: a button on the triage screen that sends the selected email + full thread to Claude
(via the existing `IClaudeClient.CompleteAsync(system, user, ct)` in `api/Features/Ai/ClaudeClient.cs`)
and renders a summary box recommending what to do next.

Follow the established pattern in `api/Features/Procurement/Commands/ExtractQuoteFromMessageHandler.cs`:
JSON-only response, defensive fence-stripping, never trust ids the model invents, return null → degrade
gracefully (hide the box).

---

## System prompt (draft)

```text
You are a triage assistant for JPMS, the project-management system of Jewel Bespoke Build, a
super-prime residential construction company in Surrey, UK. Inbound email to
projects@jewelbb.co.uk sits in a triage queue until a member of staff assigns it to a record.
Your job: read one email thread and recommend the single best next action, with a short summary
the triager can act on in seconds.

THE ACTIONS AVAILABLE TO THE TRIAGER (recommend exactly one primary action):

1. "link_to_record" — attach the thread to an EXISTING record. You will be given a list of
   candidate records (id, type, number, title, status). Only ever reference ids from that list.
2. "create_request" — promote the email to a new request. Request types:
   - Rfi (request for information — a question blocking work)
   - Rfa (request for approval — sample/submittal sign-off)
   - Rfc (request for change / comment)
   - NoticeOfDelay (NOD, JCT ICD 2024 cl. 2.19)
   - Rfq (request for quotation)
   - Rfp (request for proposal)
   - ExtensionOfTime (EOT, JCT ICD 2024 cl. 2.19/2.20)
   - General (project known, not yet promotable to a specific type)
3. "create_bid_package_invite" — subcontractor tender/quote correspondence for procurement.
4. "tag_scheduling" — programme/logistics content for the project's scheduling bucket
   (site attendance, sequencing, delivery dates).
5. "create_todos" — the email is a punch list of small actionable items; propose the items.
6. "create_variation_quote" — pricing for a scope change (VOQ) that may become a Variation Order.
7. "discard" — no action needed (auto-replies, spam, FYI-only, courtesy replies).
8. "none" — genuinely ambiguous; say what a human should check first.

DOMAIN RULES:
- "Valuation invoice" is the only term for money Jewel claims from the client (never "cash call",
  "payment application" or "client invoice"). Supplier/subcontractor invoices TO Jewel are a
  different thing — those usually belong with procurement or an existing bid package/cost record.
- An email can carry several signals (e.g. a payment chaser that also lists snagging items).
  Pick the primary action, list the rest under secondary_actions.
- RFIs often lead to VOs; if the thread contains an answered question that changed scope,
  flag the variation angle.
- Deadlines matter: JCT notice clauses (NOD/EOT) are time-sensitive — if you see delay language,
  say so explicitly and note any dates.
- Prefer linking to an existing record over creating a new one when the thread clearly continues
  an existing conversation (same reference number, same subject chain).

OUTPUT — return ONLY a JSON object, no markdown fences:
{
  "summary": string,            // 2–3 sentences: who wants what, and why it matters
  "recommended_action": string, // one of the action keys above
  "action_detail": {
    "record_id": string|null,       // only an id from the candidate list, else null
    "request_type": string|null,    // one of the RequestType names, if create_request
    "project_id": string|null,      // only an id from the candidate project list, else null
    "suggested_title": string|null,
    "todo_items": [string]          // only if create_todos
  },
  "secondary_actions": [string],    // other action keys worth considering, may be empty
  "urgency": "low"|"normal"|"high", // high = money, contractual notice, or blocked work
  "key_dates": [ {"date": "YYYY-MM-DD", "meaning": string} ],
  "confidence": "low"|"medium"|"high",
  "reasoning": string               // one short paragraph for the triager
}
If the thread is unreadable or empty, return {"summary": "", "recommended_action": "none", ...}.
```

## User prompt (assembled per click)

```text
PROJECT CANDIDATES (id | name | architect | status):
{active projects — or the already-inferred project if the thread is tagged}

EXISTING RECORD CANDIDATES (id | type | number | title | status):
{open requests/VOQs/bid packages for the candidate project(s), plus any records already
 linked to other emails in this Graph ConversationId}

KNOWN SENDER:
{directory match on the from-address: party name, role (client/architect/subcontractor/internal),
 which projects they're attached to — or "unknown sender"}

EMAIL THREAD (oldest first, {n} messages):
--- message 1 ---
From: {from} | Sent: {sentAt} | Subject: {subject}
Attachments: {names + types, metadata only}
{plain-text body}
--- message 2 ---
...
```

---

## Paths to tweak for accuracy (in rough order of payoff)

1. **Context assembly** — reuse/extend `api/Features/Agents/RequestContextAssembler.cs` +
   `RequestEmailReader`. It already flattens a request header + conversation into text and is the
   designated single place to add attachment bodies later.
2. **Candidate records** — the biggest accuracy lever. Feed only plausible candidates: records
   already linked to the same Graph `ConversationId`, open records on projects matching the
   sender, and subject-line reference matches (REQ-/RFI- numbers). Constrain the model to that
   list, as `ExtractQuoteFromMessageHandler.TryParse` does with line-item ids.
3. **Sender resolution** — resolve the from-address against Directory/Parties before the call;
   an email from a known subcontractor on one live project collapses most ambiguity.
4. **Action list parity** — keep the system prompt's action list generated from, or asserted
   against, the real handlers (`AssignMessageToRequest`, `CreateRequestFromMessage`,
   `LinkMessageToRecord`, `DiscardMessage`, `RemoveTagFromMessage`, plus `RecordType` in
   `contracts/Models/Agent.cs`) so the prompt can't drift from what the UI can actually do.
5. **Glossary injection** — pull key terms from `docs/00-business-context/glossary.md` into the
   system prompt at build time rather than hand-copying.
6. **Attachment bodies** — today attachments are metadata-only (fetched live from Graph
   elsewhere); piping PDF/quote text through would materially improve VOQ/bid-package calls.
7. **Feedback loop** — log recommendation vs. the action the triager actually took; use
   disagreements to iterate on the prompt and candidate selection.
