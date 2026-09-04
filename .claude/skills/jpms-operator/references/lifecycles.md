# Record lifecycles, statuses, numbering and mail tags

## The record chain: Request → RFI → Variation

The lineage is three stages, rendered as document tabs (`RecordTabBar`) on
every page in the chain. Only records that EXIST get a tab; the action that
creates the next stage lives on the current stage's tab. On the request page
the Request and official (RFI/NOD/EOT) tabs are local panes (deep-link
`?tab=official`); the variation tab navigates to the variation's own page.
Bid packages are NOT on the bar — they are standalone records under Bid
Package Invites (separation 2026-08-12: a variation order sets the sales side
for a cost code; a bid package groups works across cost codes by trade;
tendering runs entirely on the bid package).

### Requests (`contracts/Models/Request.cs`)

Kinds (`RequestType`): Rfi (0), Rfa (1, approval/submittal), Rfc (2,
change/comment), NoticeOfDelay (3, JCT ICD 2024 cl. 2.19), Rfq (4), Rfp (5),
ExtensionOfTime (6, cl. 2.19/2.20), General (7 — default: project-tagged, cost
centre known, not yet promoted). General requests are being sunset; nothing
raises a new one.

Status answers "whose court is the ball in?" (`RequestStatus`):

- **NeedsAction** (0) — with us: issue the document, act on the response, re-file.
- **Open** (1) — with the correspondent (architect); awaiting response.
- **NeedsVariation** (6) — the response requires a variation to be raised.
- **Closed** (4) — done.

Numbering: sequential per project, rendered `REQ-0001` (`DisplayNumber`);
RFIs render as RFI-NNN in registers. **Two dates, two meanings**: `Issued` is
the official date the correspondent was notified — lists lead with it, and it
is user-editable on requests; `Created`/`RaisedAt` is the system stamp, shown
only as a secondary fact. Never label `CreatedAt` "Raised".

### Variations (`contracts/Models/VariationOrder.cs`)

ONE document with one number through every stage — `VariationOrderStatus`:

    Quoting (0) → Issued (1) → AwaitingArchitectInstruction (4, "Awaiting AI")
    → Approved (2) / Rejected (3)

- Users read the number as **V72** (`DisplayNumber`, and the `VariationRef`
  minted at approval — same number). Stored `Reference` is `VOQ-0072`.
- Awaiting AI is a side-effect-free waiting stage: the client has the
  variation and a formal Architect's Instruction is awaited. One AI routinely
  covers several variations (linked on the AI register).
- **Approve is the money moment**: the approve modal builds priced lines (one
  per cost centre) and writes them to the Valuation Report, CVR and budgets.
  Reject after approval reverses the writes; "Return to quoting" un-approves.
  Pre-approval Rejected is terminal (confirmed first).
- The 2026-07-23 `UnifyVariationOrders` migration folded VOQ+VO into one row.
  "VOQ" survives only in persisted identifiers: the `VariationOrderQuotes`
  table and `VariationOrderQuoteId` column, `VOQ-0072` references, `JPMS/VOQ-…`
  mail tags, `/api/…/voq(s)/…` routes, `RecordType.VariationQuote`, commands
  like `CreateVoqFromRfq`. Page route is `/projects/{p}/variations/{id}`;
  `/voq/{id}` lands on the same page.

## Valuation: claims, invoices, snapshots

The live Valuation Report runs in monthly claims. Claim card stepper (one
click per material stage): **Value & lock → Claim → Approve → Invoice → Paid →
Confirm & roll over**.

Valuation invoice (`ValuationInvoiceStatus`): raised & sent in one move —
**Submitted** (with the architect/client for approval) → **Approved** →
**Issued** (counts toward certified/invoiced to date) → **Paid** (rolls into
the project's paid total). **Raised** (0) survives as a draft/recovery state;
**Rejected** returns the invoice to draft for amendment (events audit-trailed
on the same invoice — no versioning); **Cancelled** exists; projects with no
formal approval loop issue directly ("issue without approval" in the Actions
menu). Never call it a cash call, payment application or client invoice.

Snapshots freeze automatically when an invoice is raised, and again on
submit/issue after an amendment. The live report is internal-only; snapshots
are the client-facing artefact.

Correspondence: the live claim is a linkable record in the Control Centre
(Client → Valuation claims, tag `JPMS/VAL-{project}-{claim number}`), so mail
about the period files to it before anything is sent. A snapshot reads its
own tag (`JPMS/VRS-{project}-{n}`) AND its claim's, so the period's mail
travels with the statement frozen from it; Confirm & roll over starts the
next claim, whose number mints the next tag.

## Bid packages and work orders

Bid package: Draft on creation (title + trade) → lines/summary built on the
detail page → invites sent (projects mailbox, BCC, standard T&Cs attached) →
submissions recorded/extracted → **Award raises the work order**. Closed
packages sort last but stay reachable. Legacy columns
(`SelectedBidPackageId`, packages' `VariationOrderQuoteId`) are data-only.

Work order / purchase order status: Draft awaiting approval → (two-click
Approve mints the next WO number, emails the PO) Awaiting supplier acceptance
→ Accepted; or Rejected (terminal) / Cancelled (MD/FD; refused while bills
are linked or money paid). Re-code moves/splits a line across cost centres
without changing order value. An order can never be invoiced past its value.

## Defects

`DEF-####` sequential reference, which is also the mailbox tag stem. Status
walks Open → In progress → Resolved → Verified.

## Projects

`ProjectStage`: Lead → PreConstruction → Procurement → Mobilisation →
LiveDelivery → CloseOut → DefectsPeriod → Completed.

Every project list is in one order — live work first
(`ProjectOrdering.InWorkOrder()`): coarse bands Pre-Con/Procurement/
Mobilisation/Live/Close-Out (0) → Defects Period (1) → Lead (2) → Completed
(3), then A–Z, then reference. Applied once in
`ListProjectsVisibleToUserHandler`; callers that filter re-apply
`.InWorkOrder()`. Completed projects are ordered last, not hidden — except
from the side-nav switcher, prev/next cycle and finance overview, which have a
per-user "Show completed" toggle (`ProjectStageFilter`).

## Mail tags and correspondence

All correspondence lives in the shared projects mailbox; JPMS reads it in
place by category tags — triage only adds/removes tags, nothing moves.
Tag families seen in the codebase: record tags per reference (requests,
variations `JPMS/VOQ-…`, defects DEF-####, bid packages, to-dos), the
programme/scheduling bucket (`JPMS/SCH-` — "Relevant Event" tick at triage),
and the `JPMS/SubComms` family (general + Chaser / Info request / Materials /
H&S) feeding `/subcontractors/communications`. A record page reads its tagged
mail live; the Control Centre's Tagged view is where untagging lives.

## Retention

`RetentionSchedule` is the retention-release concept (releases 1 and 2 on the
project cashflow) — it is NOT the programme, despite the word "schedule"
surviving in its identifier. Retention terms are set on Project Settings.
