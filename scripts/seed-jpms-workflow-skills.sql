-- ============================================================================
-- seed-jpms-workflow-skills.sql  (2026-08-31)
-- ============================================================================
-- The nine workflow skills extracted from the site parity audit (docs/ai/11 §6)
-- — each page's UI-enforced procedure written down as doctrine the connector
-- serves with the matching actions' contracts — plus their area attachments.
-- Source of truth: docs/ai/skills/jpms/*.md (bodies inserted verbatim).
-- Idempotent: a skill key that already exists is left ALONE (the team may have
-- edited it on /admin/skills — a re-run never overwrites); attachments insert
-- only missing rows and never delete. One-off data seed — not an EF migration;
-- run via sqlcmd.
-- ============================================================================

SET NOCOUNT ON;

DECLARE @by nvarchar(256) = N'automation@jewelbb.co.uk';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = N'jpms-cash-forecast')
BEGIN
    INSERT INTO [dbo].[Skills]
        ([SkillKey], [AgentKey], [DisplayName], [Description], [Body], [Pinned], [IsActive], [Version], [UpdatedByEmail], [UpdatedAt])
    VALUES
        (N'jpms-cash-forecast', N'commercial', N'JPMS — Cash Timing Doctrine', N'The cash forecast''s and weekly cashflow''s timing doctrine. Load before reading, quoting or editing the cash forecast, project cash statements, or the 13-week weekly cashflow plan. Encodes amounts-authoritative-months-indicative, the Undated rule, the FD''s two timing knobs, timing-never-amounts, and Xero-as-home-of-payment-agreements.',
         N'---
name: jpms-cash-forecast
description: "The cash forecast''s and weekly cashflow''s timing doctrine. Load before reading, quoting or editing the cash forecast, project cash statements, or the 13-week weekly cashflow plan. Encodes amounts-authoritative-months-indicative, the Undated rule, the FD''s two timing knobs, timing-never-amounts, and Xero-as-home-of-payment-agreements."
---

# JPMS — Cash timing doctrine

## The forecast (monthly)

- **Amounts are authoritative; months are indicative.** The statement''s totals are real; WHICH
  month a flow lands in is a modelled guess. Never present a monthly phasing as a promise.
- Flows with no honest date (no practical-completion date to anchor on) sit in **Undated** and
  never touch the running balance. Overdue flows sit in the CURRENT month, never the past.
- The FD steers timing with exactly two per-project knobs: **set_next_valuation_date** (anchors
  the payment-lag count — the day matters) and **set_expected_monthly_valuation** (claims at that
  monthly rate until left-to-claim runs out; zero returns to even spread). Both change WHEN,
  never HOW MUCH — a phased total that disagrees with the statement is a defect, not judgment.
- Drawdown side only: overspent centres are never netted off; a retention release adds back only
  while still forecast.

## The weekly cashflow (13 weeks)

- The grid is Xero-seeded (bills at due/planned week, invoices at due/expected) plus manual
  items; **moving an entry changes WHEN it is paid, never how much** — the grid total always
  equals payables + receivables + items.
- **Real payment agreements live in Xero** (the bill''s planned date; the invoice''s expected
  date) — recorded once there, the grid follows. A portal placement is the fallback for
  week-to-week juggling, not the home of an agreement.
- One supplier belongs to at most one supplier group (two would double-count its bills); a group
  move is per-bill placements — a partial failure leaves the moved ones standing.
- Placements are shared truth with a who/when stamp — say who moved what when reporting the plan.
',
         0, 1, 1, @by, SYSDATETIMEOFFSET());
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = N'jpms-connector-mechanics')
BEGIN
    INSERT INTO [dbo].[Skills]
        ([SkillKey], [AgentKey], [DisplayName], [Description], [Body], [Pinned], [IsActive], [Version], [UpdatedByEmail], [UpdatedAt])
    VALUES
        (N'jpms-connector-mechanics', N'shared', N'JPMS — Connector Write Mechanics', N'Cross-cutting mechanics of writing through the connector — the rules that stop a well-meant edit erasing data. Load with any portal write. Encodes the full-record-write rule, read-before-write, absolute figures, complete lists, draft work orders having no number, and the confirm-first protocol''s spirit.',
         N'---
name: jpms-connector-mechanics
description: "Cross-cutting mechanics of writing through the connector — the rules that stop a well-meant edit erasing data. Load with any portal write. Encodes the full-record-write rule, read-before-write, absolute figures, complete lists, draft work orders having no number, and the confirm-first protocol''s spirit."
---

# JPMS — Connector write mechanics

- **Full-record writes**: many update actions (update_subcontractor, update_request_details,
  update_inventory_item, update_weekly_cashflow_item, update_architect_instruction, and kin)
  replace the record''s editable face WHOLE. Read the record first, change only what the user
  asked, and resend every other field exactly as read — a partial send ERASES the fields you
  omitted. When you did not read it, you may not write it.
- **Absolute figures, complete lists**: set_cost_code_budget takes absolute amounts (read
  current budgets first); set_xero_line_work_order_links and the skill/attachment savers take the
  COMPLETE new set — include everything that should remain, not just the change.
- **Draft work orders have no number** until approval — find them with list_work_orders by
  status, never by reference.
- **Confirm-first actions** (requiresConfirmation) refuse their first call by design: check for
  an existing record, show the user exactly what will happen — every value — and only send
  confirm true after their explicit yes in THIS conversation. The same spirit applies beyond the
  flag: anything financial, external-facing or irreversible gets stated first, performed second.
- **Relay refusals verbatim.** Validation answers and guard messages are the portal telling you
  (and the user) what is really true — never summarise them into something softer, and never
  retry a refused call unchanged.
- **Everything is logged under the user''s name.** Every call lands in Agent Activity and the
  audit trail exactly as if the user clicked it; act with that weight.
',
         0, 1, 1, @by, SYSDATETIMEOFFSET());
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = N'jpms-document-filing')
BEGIN
    INSERT INTO [dbo].[Skills]
        ([SkillKey], [AgentKey], [DisplayName], [Description], [Body], [Pinned], [IsActive], [Version], [UpdatedByEmail], [UpdatedAt])
    VALUES
        (N'jpms-document-filing', N'shared', N'JPMS — Document Filing', N'How documents move from email to their registers — Document Triage and the drawing register''s conventions. Load before filing attachments to Drawings, Payment Certificates or subcontractor compliance, registering drawings, or reasoning about revisions. Encodes revision inheritance, folder-first filing, the current-revision rule, date-only UTC certificate dates, and discard-never-delete.',
         N'---
name: jpms-document-filing
description: "How documents move from email to their registers — Document Triage and the drawing register''s conventions. Load before filing attachments to Drawings, Payment Certificates or subcontractor compliance, registering drawings, or reasoning about revisions. Encodes revision inheritance, folder-first filing, the current-revision rule, date-only UTC certificate dates, and discard-never-delete."
---

# JPMS — Document filing

## Document Triage

- Every item is ONE email attachment copy, waiting to be filed to exactly one home: a drawing
  (file_document_as_drawing), a payment certificate (file_document_as_payment_certificate), or a
  subcontractor''s compliance documents (file_document_to_subcontractor).
- Filing as a drawing REVISION inherits code and title from the target drawing — the item''s own
  name may be junk; the register''s identity wins. Filing as a NEW drawing resolves (or creates)
  its folder FIRST, then files into it.
- Certificate dates and compliance expiry dates are date-only, pinned to UTC — the stored day
  must never drift with anyone''s timezone. Send plain yyyy-MM-dd.
- Discard is restorable and filed rows keep their where-it-went history — nothing in this queue
  is ever deleted. When unsure where something files, leave it Pending and ask; a wrongly filed
  certificate misstates what the client certified.

## The drawing register

- A drawing''s CURRENT revision is its approved one, else its newest — trust the register''s
  hasApprovedRevision flag, not label text.
- Registering a drawing (metadata) and adding a revision (the file) are separate acts; revision
  files only arrive by upload or from Document Triage, never invented.
- Approval is evidential — it records who approved and supersedes the previous approved revision.
  Never mark approval on anyone''s behalf without their explicit say-so in this conversation.
- Deleting a REVISION and deleting the DRAWING are different destructive acts; both need the
  user''s confirmed intent, named by drawing code.
',
         0, 1, 1, @by, SYSDATETIMEOFFSET());
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = N'jpms-email-triage')
BEGIN
    INSERT INTO [dbo].[Skills]
        ([SkillKey], [AgentKey], [DisplayName], [Description], [Body], [Pinned], [IsActive], [Version], [UpdatedByEmail], [UpdatedAt])
    VALUES
        (N'jpms-email-triage', N'shared', N'JPMS — Email Triage', N'How Jewel triages the projects mailbox — the Control Centre''s procedure translated for the connector. Load before working the triage queue: listing untriaged mail, filing emails to records, raising records from emails, discarding, or replying. Encodes the apply ordering (file everything before any reply), the one-create-per-pass rule, attachments-before-body, the pane-choice-is-the-decision cross-filing rule, and when a thread counts as handled.',
         N'---
name: jpms-email-triage
description: "How Jewel triages the projects mailbox — the Control Centre''s procedure translated for the connector. Load before working the triage queue: listing untriaged mail, filing emails to records, raising records from emails, discarding, or replying. Encodes the apply ordering (file everything before any reply), the one-create-per-pass rule, attachments-before-body, the pane-choice-is-the-decision cross-filing rule, and when a thread counts as handled."
---

# JPMS — Email triage

The Control Centre stages a whole email''s decisions and lands them in one Apply. Over the
connector you perform the same decisions as individual actions — so the ORDER the page enforced
by machinery, you must enforce by discipline.

## The order of work on one email

1. **Read the thread, not just the message** (list_mailbox_conversation). Later replies often say
   how the earlier messages should be triaged. If the thread already carries record tags
   (threadTags), the decision is usually to file to the same record — one step, not a re-triage.
2. **Attachments before body** (doc-first rule). If the email carries drawings, certificates or
   compliance documents, send them to Document Triage (send_attachments_to_document_control)
   before acting on the words.
3. **To-dos next** — anything the email demands of the team (create_todos_from_message).
4. **File to records** (file_email_to_record) — every record the email genuinely concerns. An
   email can feed a request AND a cost centre AND the programme at once; multiple filings are
   normal, not a smell.
5. **Create at most ONE new record per email per pass** (create_request_from_message,
   create_work_order_from_message, create_defect_from_message, log_tender_enquiry_from_message,
   create_inventory_item_from_message, …). Creating mints the record''s tag onto the email, so the
   next filing decision sees it. If an email seems to need two new records, do the second on a
   second pass, after the first exists.
6. **Discard only what needs nothing** (discard_mailbox_message) — circulars, pure
   acknowledgements. Discard is restorable; when in doubt, file rather than discard.
7. **Replies LAST, and only as drafts.** Everything above must be filed before any reply is
   prepared, so a failed send loses nothing already filed. The connector never sends email —
   prepare_*_draft actions stage a draft in the shared mailbox and the human sends from Outlook.

## Decisions, not defaults

- **The pathway choice IS the decision.** Filing a Subcontractor-pathway thread onto a client
  record (or the reverse) is allowed — but it is a cross-filing the user must confirm. When an
  action answers that the thread is already filed under another pathway, ask the user; never
  silently pass allowCrossPathway true on your own judgment.
- **Project match is a guess until confirmed.** The queue''s project hint comes from the email;
  say which project you are filing under and let the user correct it.
- **A thread is "handled" when its business is filed**, not when it has been read. Do not chase
  an inbox-zero count; chase every email''s business landing on the right record.

## What you cannot do (by design)

Sending email (all paths are draft-then-human-sends) and bulk retagging are portal-only. Say so
rather than improvising.
',
         0, 1, 1, @by, SYSDATETIMEOFFSET());
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = N'jpms-labour-rules')
BEGIN
    INSERT INTO [dbo].[Skills]
        ([SkillKey], [AgentKey], [DisplayName], [Description], [Body], [Pinned], [IsActive], [Version], [UpdatedByEmail], [UpdatedAt])
    VALUES
        (N'jpms-labour-rules', N'timesheets', N'JPMS — Labour Rules', N'Labour and timesheet doctrine — how hours become cost and what is immutable. Load before any timesheet, worker, absence or labour-cost work. Encodes view-code-approve order, approval immutability, the budget hard-block, rate confidentiality, sign-off freezing, and close-and-replace mappings.',
         N'---
name: jpms-labour-rules
description: "Labour and timesheet doctrine — how hours become cost and what is immutable. Load before any timesheet, worker, absence or labour-cost work. Encodes view-code-approve order, approval immutability, the budget hard-block, rate confidentiality, sign-off freezing, and close-and-replace mappings."
---

# JPMS — Labour rules

## The order: view → code → approve

1. **view_labour_week first** — see the week''s submitted days and their coding state.
2. **Code before approving** (code_worker_week): uncoded days REFUSE approval.
3. **approve_worker_week posts cost.** Approval snapshots the worker''s rate effective on the
   worked date and posts the hours to Financials as actual labour cost. An approved timesheet is
   IMMUTABLE — hours and cost code can never change afterwards; the correction path is
   reject-and-resubmit (reject_worker_day, with a reason the worker reads) or a settlement
   variance. Never promise an edit to an approved row.

## Money rules

- **The budget hard-block is server-enforced**: approval is refused for a cost code whose
  remaining budget the new cost would exceed, and the refusal reports the code''s figures. Relay
  the refusal; never route around it.
- Only APPROVED time is cost. Submitted time is exposure; quote them separately.
- **Rates are confidential to managing roles** — worker rates (list_workers) never reach site
  surfaces or any output a site role or subcontractor will see. Rate changes apply to FUTURE
  approvals only; history keeps its snapshots.
- Weekly sign-off freezes a worker-week before settlement; the Xero coding run refuses unmapped
  sites and codes by name — the fix is the mapping, not a guess.
- Xero mappings are effective-dated bridges: setting one CLOSES the old row and starts a new one
  (never edits), so historic reads still translate.
',
         0, 1, 1, @by, SYSDATETIMEOFFSET());
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = N'jpms-tender-award')
BEGIN
    INSERT INTO [dbo].[Skills]
        ([SkillKey], [AgentKey], [DisplayName], [Description], [Body], [Pinned], [IsActive], [Version], [UpdatedByEmail], [UpdatedAt])
    VALUES
        (N'jpms-tender-award', N'bid-packages', N'JPMS — Tender & Award', N'The bid-package tender flow from scope to purchase order. Load before building bid packages, handling incoming quotes, awarding, or raising the post-award work order. Encodes extract-never-hand-type for quotes, the human-sends-invites rule, directory hygiene for tender-only prospects, award-mints-the-WO, and the PO email as a distinct second step.',
         N'---
name: jpms-tender-award
description: "The bid-package tender flow from scope to purchase order. Load before building bid packages, handling incoming quotes, awarding, or raising the post-award work order. Encodes extract-never-hand-type for quotes, the human-sends-invites rule, directory hygiene for tender-only prospects, award-mints-the-WO, and the PO email as a distinct second step."
---

# JPMS — Tender and award

## The flow

1. **Build the package**: scope (update_bid_package_scope), line items, drawings. A bid package
   is a STANDALONE record grouping works across cost codes by trade — never a stage of the
   variation chain.
2. **Invite**: add recipients (invite_subcontractors_to_bid_package). The invite EMAIL itself is
   sent by a human from the portal — prepare everything, then hand over.
3. **Quotes arrive by email.** NEVER hand-type a quote''s figures: run extract_tender_from_message
   on the email, review what it extracted with the user, then save_extracted_quote. A typo in a
   tender figure survives into the award and the work order.
4. **Award** (award_bid_package — confirm-first): awarding mints the work order to the chosen
   subcontractor.
5. **The PO email is a distinct second step** (prepare_work_order_email_draft): a draft in the
   shared mailbox for the human to review and send — the tool never sends.

## Directory hygiene

Tender-only prospects are NOT directory members. Promote a company into the directory only from
a submitted tender or at award (promote_subcontractor_to_directory) — the directory stays a
curated list of firms Jewel actually works with. Renaming a directory company to match its Xero
supplier name is what lines its invoices up on the allocation side.

## Quoting discipline

Never disclose one bidder''s figures to another, and never put subcontractor pricing in anything
client-bound. Comparisons live in internal working documents only.
',
         0, 1, 1, @by, SYSDATETIMEOFFSET());
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = N'jpms-valuation-cycle')
BEGIN
    INSERT INTO [dbo].[Skills]
        ([SkillKey], [AgentKey], [DisplayName], [Description], [Body], [Pinned], [IsActive], [Version], [UpdatedByEmail], [UpdatedAt])
    VALUES
        (N'jpms-valuation-cycle', N'commercial', N'JPMS — Valuation Cycle', N'The monthly valuation claim and invoice cycle — the money path from % complete to cash. Load before any valuation, claim or valuation-invoice work: recording progress, preapproving, raising/submitting/issuing invoices, payments, or presenting a statement to anyone. Encodes the claim stepper, the frozen-snapshot client rule, cumulative seeding, server-stamped retention, and what certified-to-date means.',
         N'---
name: jpms-valuation-cycle
description: "The monthly valuation claim and invoice cycle — the money path from % complete to cash. Load before any valuation, claim or valuation-invoice work: recording progress, preapproving, raising/submitting/issuing invoices, payments, or presenting a statement to anyone. Encodes the claim stepper, the frozen-snapshot client rule, cumulative seeding, server-stamped retention, and what certified-to-date means."
---

# JPMS — The valuation cycle

## The stepper (one claim, in order)

1. **Value the month**: record cumulative % complete per line (claim_progress /
   record_claim_entries). New claims ALWAYS seed from the latest claim''s cumulative position —
   never start a month from zero.
2. **Lock**: preapprove_valuation_claim freezes the month''s figures for claiming.
3. **Raise & send**: create the valuation invoice and submit it — raising freezes a REPORT
   SNAPSHOT; that frozen statement is what the client is sent, backing this invoice.
4. **Approval**: record the client''s approval (or rejection — a rejected invoice returns to
   draft for amend-and-resend). "Issue without approval" is legitimate only for clients with no
   formal approval loop — ask before using it.
5. **Issue**: issuing is what moves CERTIFIED-TO-DATE. Until issued, the money is exposure, not
   certification.
6. **Payment**: record it when it lands. Payment is NOT a gate for starting the next claim — the
   next month begins on its own clock.
7. **Confirm & roll over**: confirming closes the claim into history. Confirming without an
   issued invoice earns a nudge, not a block — mention it to the user.

## Non-negotiables

- **The client sees the FROZEN snapshot, never the live report.** The live report is a working
  copy; anything presented, emailed or quoted as "the valuation" must come from the snapshot
  behind the invoice (get_valuation_snapshot). Comparing live vs frozen is how you answer "what
  moved since we claimed".
- **Retention is stamped server-side** from the project''s terms — never compute or pass it.
- **Certified-to-date = issued + paid invoices (gross of deposit credits).** Quote it from
  list_valuation_invoices'' summary, never by adding numbers yourself.
- Deleting claims or invoices is recovery machinery, not tidying — user''s explicit say-so, named
  by number, every time.
',
         0, 1, 1, @by, SYSDATETIMEOFFSET());
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = N'jpms-variation-lifecycle')
BEGIN
    INSERT INTO [dbo].[Skills]
        ([SkillKey], [AgentKey], [DisplayName], [Description], [Body], [Pinned], [IsActive], [Version], [UpdatedByEmail], [UpdatedAt])
    VALUES
        (N'jpms-variation-lifecycle', N'commercial', N'JPMS — Variation Lifecycle', N'The variation''s one-document lifecycle and the staged build-up doctrine. Load before pricing, staging, approving, revising or reporting on variations, or working an Awaiting-AI position. Encodes one-number-through-every-stage, stage-then-USER-approves, what approval mints, evidence via architect instructions, and the work-order fallback.',
         N'---
name: jpms-variation-lifecycle
description: "The variation''s one-document lifecycle and the staged build-up doctrine. Load before pricing, staging, approving, revising or reporting on variations, or working an Awaiting-AI position. Encodes one-number-through-every-stage, stage-then-USER-approves, what approval mints, evidence via architect instructions, and the work-order fallback."
---

# JPMS — The variation lifecycle

## One document, one number

A variation is ONE record through every stage; a user reads it as **V72**. Its status says where
it has got to: Quoting → Issued → Awaiting AI → Approved / Rejected. Never speak of "VOQ" and
"VO" as two things, and never invent a second number.

## Pricing and approval

- **Stage, don''t approve.** Build the priced lines with stage_variation_order_build_up — the
  staged TOTAL becomes the estimate, and the portal''s approve panel opens pre-seeded. The USER
  presses approve. Only call approve_variation_order when the user has explicitly said, in this
  conversation, to approve that variation by number.
- **Approval mints the V-ref and mirrors the priced lines onto the Valuation Report** under the
  V-number, writes the QS accrual and commits budget. Revising after approval
  (revise_variation_order_lines / revise_variation_order_value) is a REAL financial act — the
  commercial records move by the difference; treat it with approval-grade care.
- **Awaiting AI means waiting for an Architect''s Instruction.** The evidence lives in the
  instruction register: check list_architect_instructions for coverage, file the instruction from
  its email when it lands (import_architect_instruction_from_message), link it, THEN the
  variation can move.
- Pre-approval estimate changes use set_variation_order_estimate; the status ladder''s
  side-effect-free moves use set_variation_order_status; rejection and return-to-quoting keep
  the same document alive.

## After approval

Issuing a work order straight from a variation is portal-only — fall back to
create_manual_work_order against the variation''s cost centres, or tell the user to click it.
Client-facing variation documents show contract-basis rates; never expose subcontractor costs or
margin in anything client-bound (the commercial doctrine skills govern the wording).
',
         0, 1, 1, @by, SYSDATETIMEOFFSET());
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Skills] WHERE [SkillKey] = N'jpms-xero-allocation')
BEGIN
    INSERT INTO [dbo].[Skills]
        ([SkillKey], [AgentKey], [DisplayName], [Description], [Body], [Pinned], [IsActive], [Version], [UpdatedByEmail], [UpdatedAt])
    VALUES
        (N'jpms-xero-allocation', N'commercial', N'JPMS — Xero Allocation Doctrine', N'How Xero and the portal reconcile — allocation doctrine and why the ledgers read the way they do. Load before reading or discussing Xero costs, aged payables/receivables, cost-of-sales spend, work-order invoice links, or anything built on allocated lines. Encodes drafts-are-deliberate, allocation-moves-money-views, the complete-slice-list rule, and labour-settlement bills.',
         N'---
name: jpms-xero-allocation
description: "How Xero and the portal reconcile — allocation doctrine and why the ledgers read the way they do. Load before reading or discussing Xero costs, aged payables/receivables, cost-of-sales spend, work-order invoice links, or anything built on allocated lines. Encodes drafts-are-deliberate, allocation-moves-money-views, the complete-slice-list rule, and labour-settlement bills."
---

# JPMS — Xero allocation doctrine

## Why the portal''s numbers beat Xero''s reports

- **Draft bills are deliberate.** The coding procedure holds purchase bills in DRAFT until they
  are allocated through the portal, so Xero''s own aged-payables report UNDERCOUNTS what is owed.
  Quote get_aged_payables / get_aged_receivables (drafts included), never Xero''s report, for what
  we owe or are owed.
- **A cost only reaches a project when its line is ALLOCATED** to a project + master cost centre
  (or split across several). Unallocated lines are real money not yet in any project''s spend —
  when project cost figures look light, check the Unallocated queue first
  (list_xero_ledger_lines).

## Allocation rules (the page''s arming rules, as doctrine)

- A line goes to ONE of: project + cost centre, or a bucket (no project, no cost centre) —
  when both are somehow set, the bucket was the later deliberate act and wins.
- Half-allocated states exist on purpose: a line can carry its project (moving it to that
  project''s tab and writing the Xero site) without being allocated to a centre yet.
- Disputed lines are a conversation, not an error state — the thread survives resolution.
- **Labour-registry suppliers'' bills are timesheet SETTLEMENT, not costs** — they bypass the
  allocation queue entirely; labour cost enters projects through approved timesheets.

## Work-order invoice links

set_xero_line_work_order_links takes the line''s COMPLETE slice list every time — read the
current links first (list_xero_ledger_lines with the projectId), modify, resend the whole set.
Sending a partial list silently drops the missing allocations.
',
         0, 1, 1, @by, SYSDATETIMEOFFSET());
END;

-- Area attachments for the nine skills (26 rows).
INSERT INTO [dbo].[AiActionSkills]
    ([ActionSkillId], [TargetKind], [TargetKey], [SkillKey], [AttachedByEmail], [AttachedAt])
SELECT
    LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), N'-', N'')),
    N'area',
    v.[TargetKey],
    v.[SkillKey],
    @by,
    SYSDATETIMEOFFSET()
FROM (VALUES
    (N'Correspondence', N'jpms-email-triage'),
    (N'Requests & RFIs', N'jpms-email-triage'),
    (N'Document control', N'jpms-document-filing'),
    (N'Drawings', N'jpms-document-filing'),
    (N'Commercial', N'jpms-valuation-cycle'),
    (N'Valuation invoices', N'jpms-valuation-cycle'),
    (N'Architect instructions', N'jpms-variation-lifecycle'),
    (N'Variations', N'jpms-variation-lifecycle'),
    (N'Procurement', N'jpms-tender-award'),
    (N'Cashflow', N'jpms-xero-allocation'),
    (N'Cost centres', N'jpms-xero-allocation'),
    (N'Cashflow', N'jpms-cash-forecast'),
    (N'Labour', N'jpms-labour-rules'),
    (N'Architect instructions', N'jpms-connector-mechanics'),
    (N'Cashflow', N'jpms-connector-mechanics'),
    (N'Commercial', N'jpms-connector-mechanics'),
    (N'Contacts', N'jpms-connector-mechanics'),
    (N'Correspondence', N'jpms-connector-mechanics'),
    (N'Cost centres', N'jpms-connector-mechanics'),
    (N'Directory & users', N'jpms-connector-mechanics'),
    (N'Inventory', N'jpms-connector-mechanics'),
    (N'Procurement', N'jpms-connector-mechanics'),
    (N'Requests & RFIs', N'jpms-connector-mechanics'),
    (N'Subcontractors', N'jpms-connector-mechanics'),
    (N'Valuation invoices', N'jpms-connector-mechanics'),
    (N'Variations', N'jpms-connector-mechanics')
) AS v ([TargetKey], [SkillKey])
INNER JOIN [dbo].[Skills] s
    ON s.[SkillKey] = v.[SkillKey]
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[AiActionSkills] existing
    WHERE existing.[TargetKind] = N'area'
      AND existing.[TargetKey] = v.[TargetKey]
      AND existing.[SkillKey] = v.[SkillKey]
);

PRINT CONCAT('Attached ', @@ROWCOUNT, ' area-skill rows for the workflow skills.');

SELECT [SkillKey], [AgentKey], [Version] FROM [dbo].[Skills] WHERE [SkillKey] LIKE N'jpms-%' ORDER BY [SkillKey];
