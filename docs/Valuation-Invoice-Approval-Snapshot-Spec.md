# Valuation invoice approval, report snapshots & historic amounts — design spec

**Status:** Draft for sign-off
**Author:** Cowork (for Nigel Reilly)
**Date:** 10 July 2026
**Builds on:** `Valuation-Report-Tab-Spec.md` (shipped), `Entity-Refactor-Request-VO-Valuation-Plan.md`

---

## 1. What we're building

Three related capabilities on top of the shipped Valuation Report tab and valuation-invoice machinery:

1. **Approval workflow** on valuation invoices. Today an invoice goes Raised → Issued → Paid with no room for the period where it sits with the architect/client and may come back rejected or needing amendment. We add Submitted / Approved / Rejected states, amendment on the same invoice, and full CRUD.
2. **Valuation report snapshots.** When an invoice is submitted for approval, the system freezes a complete, immutable copy of the valuation report — every line with its % complete and cumulative claimed, plus the summary/retention footer. Snapshots are listable, viewable, and deletable, and each invoice links to the snapshot it was raised against ("show me the report behind VI-0007"). Snapshots can also be taken on demand as a period-end record.
3. **Manual (historic) invoices.** Backdated invoices — period, amount, amount paid, dates — entered directly as Issued or Paid so the years of receipts to date can be brought current. They count fully toward Certified-to-date and Total Paid, exactly like system-raised invoices, so every future valuation computes the correct balance outstanding.

### Why this is a small change to the maths

`ValuationClaimSummary.ApplyTotalsAsync` already derives **Certified to date = Σ Amount of Issued + Paid invoices** for the project, and `PreapprovedClaimTotals.RefreshAsync` already re-freezes Preapproved claims whenever that figure moves. Manual invoices therefore need **no calculation changes** — a backdated Paid invoice feeds Certified-to-date the moment it's saved, and the next claim's

```
Payment Due (ex VAT) = Total Works Complete − Retention Held + Retention Released − Certified to Date
```

comes out right automatically. The new Submitted/Approved states deliberately do **not** count toward Certified-to-date (see §3.3), so the summary maths in `ValuationCalculations.cs` is untouched.

---

## 2. Current state (recap)

| Piece | Today | Gap |
|---|---|---|
| `ValuationInvoiceStatus` | `Raised(0) → Issued(1) → Paid(2)` | No approval window, no rejection, no amendment trail |
| Invoice commands | Create, Issue, RecordPayment, Delete | No Update, no Submit/Approve/Reject, no manual/backdated entry |
| Report freezing | `ValuationClaim` freezes **summary totals** on Confirm; Preapproved totals are *re-frozen* when invoices change | No immutable, line-level copy of the report; nothing an invoice can point back to |
| UI (`ValuationInvoicesSection.razor`) | List, add (with "already paid" checkbox), issue, record payment, delete | No status pipeline, no snapshot viewer, no historic-entry form, no edit |

Note the existing claim is **not** a snapshot: a Preapproved claim's totals are recomputed whenever Certified-to-date moves, and Draft claims compute live. That's correct for the working report, which is exactly why a separate immutable snapshot entity is needed for "what did we show the client when we asked for this money".

---

## 3. Valuation invoice lifecycle (revised)

### 3.1 Status model

```
                        ┌──────────── amend (edit ⇒ back to Raised) ───────────┐
                        ▼                                                      │
  Raised ──submit──▶ Submitted ──approve──▶ Approved ──issue──▶ Issued ──pay──▶ Paid
    │                   │
    │                   └──reject──▶ Rejected ──┬── amend ⇒ Raised
    │                                           └── cancel ⇒ Cancelled
    └── cancel ⇒ Cancelled
```

`ValuationInvoiceStatus` gains values **without renumbering** the existing three (they're persisted as ints and seeded):

```csharp
public enum ValuationInvoiceStatus
{
    Raised = 0,     // draft — editable
    Issued = 1,     // client invoice sent — counts toward Certified-to-date
    Paid = 2,       // receipt recorded — rolls into project paid total
    Submitted = 3,  // with the architect/client for approval — locked, snapshot taken
    Approved = 4,   // client has approved the amount — locked, awaiting formal issue
    Rejected = 5,   // client rejected — unlocked for amendment or cancellation
    Cancelled = 6   // withdrawn; kept for the audit trail, excluded from all totals
}
```

### 3.2 Transition rules

| From | Action | To | Effects |
|---|---|---|---|
| Raised | **Submit** | Submitted | Takes a `ValuationReportSnapshot` (§4) and links it; stamps `SubmittedAt`; locks amount/period |
| Submitted | **Approve** | Approved | Stamps `ApprovedAt`; optionally records approved-by note |
| Submitted | **Reject** | Rejected | Stamps `RejectedAt` + required `RejectionReason`; invoice unlocked |
| Rejected / Raised | **Amend** (edit amount/period) | Raised | Increments `AmendmentCount` when the invoice has been submitted or rejected before (editing a never-submitted draft is just an edit); prior snapshot marked superseded; history row records old→new amount |
| Approved | **Issue** | Issued | Stamps `IssuedAt`; **now** counts toward Certified-to-date; `PreapprovedClaimTotals.RefreshAsync` runs |
| Issued | **Record payment** | Paid | As today |
| Raised / Rejected | **Cancel** | Cancelled | Terminal; excluded from every total |
| any (backoffice) | **Delete** | — | As today: Certified-to-date and paid totals roll back, Preapproved claims re-freeze; also deletes linked snapshots |

Direct Raised → Issued stays permitted (skip-approval path) so projects that don't run a formal approval loop keep today's two-click flow. Manual invoices (§5) are created directly in Issued or Paid and never enter the approval pipeline.

### 3.3 What counts when

Certified-to-date remains **Issued + Paid only**. A Submitted or Approved invoice is a *pending* claim on the client — showing it in Certified-to-date early would understate the next Payment Due and double-discount if it's then rejected. The summary card gains a separate **"Awaiting approval"** figure (Σ Submitted + Approved) so the exposure is still visible.

`ProjectValuationInvoiceSummary` gains `TotalAwaitingApproval`; `TotalRaised` continues to exclude Cancelled.

### 3.4 Amendment trail (same invoice, no versioning)

Per the decision to track amendments on the same invoice rather than superseding versions, we add a lightweight history table rather than invoice copies:

```
ValuationInvoiceEvent
  ValuationInvoiceEventId, ValuationInvoiceId,
  EventType: Created | Submitted | Approved | Rejected | Amended | Issued |
             PaymentRecorded | Cancelled | ManualEntry,
  OccurredAt, Note,            // e.g. rejection reason, amendment summary
  AmountBefore?, AmountAfter?  // populated for Amended / PaymentRecorded
```

Written by every command handler in the same transaction. The UI shows it as an expandable "History" row per invoice. This also gives the manual entries an auditable "entered by hand on \<date\>" record.

### 3.5 Model changes

`ValuationInvoice` gains: `SubmittedAt?`, `ApprovedAt?`, `RejectedAt?`, `CancelledAt?`, `RejectionReason?`, `AmendmentCount`, `IsManual`, `ValuationReportSnapshotId?` (current/latest snapshot). Existing fields, `DisplayNumber`, and the `ValuationClaimId` link are unchanged.

---

## 4. Valuation report snapshots

### 4.1 Model

Two new records in `contracts/Models/ValuationReport.cs` (+ entities, DbSets, migration):

```
ValuationReportSnapshot
  ValuationReportSnapshotId, ProjectId,
  ValuationInvoiceId?,        // the invoice this submission backs (null for on-demand)
  ValuationClaimId?,          // the claim the figures came from, if one was open
  Label,                      // e.g. "VI-0007 submission" / "June 2026 period end"
  TakenAt, IsSuperseded,      // superseded = a later snapshot exists for the same invoice
  // frozen summary footer:
  ContractSum, NetVariations, RevisedContractSum,
  TotalWorksComplete,
  RetentionPercent, RetentionHeld,
  RetentionReleasePercent, RetentionReleased,
  CertifiedToDate, PaymentDueExVat

ValuationReportSnapshotLine     // full frozen copy of every priced row
  ValuationReportSnapshotLineId, ValuationReportSnapshotId,
  SourceValuationLineItemId,    // provenance only — not a FK dependency
  ElementType, SectionCode, SectionName, VariationRef, VariationTitle,
  LineType, CostCode, Description, Unit, Quantity, Rate, LineAmount,
  PercentComplete, CumulativeClaimed, PeriodIncrement,
  Comments, DisplayOrder
```

Snapshot lines copy **values, not references**, so later edits or deletions of live `ValuationLineItem`s never disturb what was submitted. `CertifiedToDate` is stamped from the same Issued+Paid query the claim summary uses, at the moment of capture.

### 4.2 When snapshots are taken

- **Automatically on Submit** (§3.2) — freezes exactly what the client is being asked to approve.
- **Automatically on re-submit after amendment** — a fresh snapshot is taken; the previous one is flagged `IsSuperseded` but retained, so the trail of what was asked for each time survives.
- **On demand** — a "Take snapshot" action on the Valuation Report tab records a period-end copy with a free-text label, no invoice link. This is the monthly (or any-period) snapshot record.

### 4.3 CRUD semantics

Snapshots are **immutable once taken**: create (capture), read (list + full report view), delete. No update — an amended submission produces a new snapshot rather than editing the old one. Delete is available for snapshots taken in error; deleting an invoice cascades to its snapshots. Deleting a snapshot never touches live data.

### 4.4 Viewing

- **Snapshots list** on the Valuation Report tab: TakenAt, Label, linked invoice (chip → invoice row), Payment Due, superseded badge.
- **Snapshot viewer**: renders the frozen report read-only using the existing `ValuationReportTable` layout (grouped bill → PC sums → contingency → variations → summary footer), fed from snapshot lines instead of live data, with a banner "Snapshot taken 10 Jul 2026 — VI-0007 submission".
- **From an invoice**: a "View report" link on each invoice row opens the (latest) linked snapshot — this is the "see the valuation report associated with a valuation invoice" requirement. Superseded snapshots are reachable from the invoice's history.

---

## 5. Manual (historic) invoices

A dedicated "Add historic invoice" form (extending the existing add-row in `ValuationInvoicesSection`):

| Field | Notes |
|---|---|
| Period month | backdated freely |
| Amount | invoiced amount ex VAT |
| Amount paid | defaults to Amount; may be partial |
| Issued date / Paid date | backdated; drive `IssuedAt`/`PaidAt` |
| Reference note | e.g. original invoice number from the old system |

Behaviour:

- Created via `CreateValuationInvoice` extended with `IsManual`, `AmountPaid`, `IssuedAt`, `PaidAt`, `Note`; status computed: Paid if a paid amount/date is given, else Issued.
- Numbered in the normal `VI-nnnn` sequence, flagged **Manual** in the list (badge), with a `ManualEntry` history event.
- Counts toward Certified-to-date and Total Paid identically to system-raised invoices — no special-casing in `ValuationClaimSummary`. After save, `PreapprovedClaimTotals.RefreshAsync` runs so any open Preapproved claim re-freezes.
- No snapshot and no approval pipeline: these record history, they don't request funds.
- Editable via `UpdateValuationInvoice` (amount, paid amount, dates, period) with the same refresh, since correcting historic figures is the point; each edit writes an `Amended` history event.

Result: once the back-history is keyed in, the next claim's Certified-to-date equals everything genuinely invoiced over the years, and Payment Due shows the true balance outstanding.

---

## 6. Contracts & API surface

New/changed under `contracts/ValuationInvoices/`:

| Contract | Kind | Notes |
|---|---|---|
| `CreateValuationInvoice` | change | + `IsManual`, `AmountPaid?`, `IssuedAt?`, `PaidAt?`, `Note?` |
| `UpdateValuationInvoice` | new | amount/period (+ paid/dates for manual); allowed in Raised, Rejected, or any manual invoice |
| `SubmitValuationInvoice` | new | Raised → Submitted; captures + links snapshot |
| `ApproveValuationInvoice` | new | Submitted → Approved |
| `RejectValuationInvoice` | new | Submitted → Rejected; requires reason |
| `CancelValuationInvoice` | new | Raised/Rejected → Cancelled |
| `ListValuationInvoiceEvents` | new | history for one invoice |
| `IssueValuationInvoice` | change | allowed from Approved **or** Raised (skip path) |
| `GetProjectValuationInvoiceSummary` | change | + `TotalAwaitingApproval` |

New under `contracts/Commercial/` (snapshots live with the report feature):

| Contract | Kind |
|---|---|
| `TakeValuationReportSnapshot(ProjectId, Label, ValuationInvoiceId?)` | new command |
| `ListValuationReportSnapshotsForProject(ProjectId)` | new query |
| `GetValuationReportSnapshot(SnapshotId)` | new query (header + lines) |
| `DeleteValuationReportSnapshot(SnapshotId)` | new command |

API: handlers/endpoints/validation/authorisation mirroring the existing sets in `api/Features/ValuationInvoices/` and `api/Features/Commercial/`; snapshot capture logic shared between `SubmitValuationInvoice` and `TakeValuationReportSnapshot` handlers (one internal `SnapshotCapture` helper that reads live lines + the open claim's entries and the Issued+Paid certified query). One EF migration: `AddInvoiceApprovalAndReportSnapshots` (new columns on `ValuationInvoices`, two snapshot tables, events table).

---

## 7. Front-end

Per the CLAUDE.md store convention, all new stores fetch once per key with `Refresh(projectId)` called from `OnInitializedAsync` only (stale-while-revalidate).

1. **`IValuationInvoiceStore`** — add `UpdateAsync`, `SubmitAsync`, `ApproveAsync`, `RejectAsync`, `CancelAsync`, `ListEventsAsync`.
2. **`IValuationReportStore`** — add snapshot members: `SnapshotsFor(projectId)`, `TakeSnapshotAsync`, `GetSnapshotAsync`, `DeleteSnapshotAsync`.
3. **`ValuationInvoicesSection.razor`** — status pipeline chips (Raised / Submitted / Approved / Rejected / Issued / Paid / Cancelled, Manual badge); per-status actions (Submit, Approve, Reject with reason, Amend, Issue, Record payment, Cancel, Delete); "View report" link to the linked snapshot; expandable history row; "Awaiting approval" figure in the header; "Add historic invoice" form.
4. **Valuation Report tab** — "Take snapshot" action + snapshots list; snapshot viewer (modal or `/projects/{id}/valuation/snapshots/{snapshotId}`) reusing `ValuationReportTable` in read-only mode with a snapshot banner.

---

## 8. Verification plan

1. Unit tests on transition rules (every legal/illegal edge of §3.2), including that Submitted/Approved never move Certified-to-date and Cancelled leaves every total.
2. Snapshot immutability test: take snapshot → edit/delete live lines → snapshot figures unchanged; amend + resubmit → two snapshots, first superseded.
3. Manual-entry regression: seed a project, key three backdated Paid invoices, confirm the next claim's CertifiedToDate and PaymentDueExVat match hand-computed figures (By France-style reconciliation).
4. Delete rollback paths (existing behaviour) still pass with the new states present.
5. Build + render check of the updated tab and snapshot viewer.

---

## 9. Decisions taken (per sign-off discussion)

1. **Approval flow** → Raised → Submitted → Approved → Issued → Paid, with Rejected returning for amendment or cancellation; amendments tracked on the same invoice (event history), no invoice versioning.
2. **Snapshot scope** → full line-level report copy plus summary footer, immutable, taken on submit and on demand.
3. **Historic amounts** → individual backdated invoices counting fully toward Certified-to-date; no opening-balance shortcut.
4. **Enum numbering** → existing Raised/Issued/Paid int values preserved; new states appended.

## 10. Open questions

1. Should **Approve** be recordable with the client's certificate reference/date (architects often certify a different figure)? If so, Approve could accept an optional `CertifiedAmount` that amends the invoice amount in one step.
2. Snapshot on **Issue** as well as Submit for the skip-approval path, so even two-click invoices get a linked report? (Cheap to add; recommended default: yes.)
3. Retention release remains a constant 0 in `ValuationClaimSummary` — out of scope here, but the snapshot stores `RetentionReleasePercent`/`RetentionReleased` so it's ready when release events land.
