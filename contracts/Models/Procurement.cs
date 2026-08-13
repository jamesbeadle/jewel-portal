namespace Jewel.JPMS.Models;

// Persisted as ints, so values are explicit. Comparing (3) was removed 2026-07-22: nothing ever
// set it — comparing tenders is a view over a QuotesReceived package, not a state of its own —
// and Awarded keeps its stored value of 4. Closed (5, added 2026-08-12) is the no-winner outcome:
// the tender process ended without an award (all declined, re-scoped, works lapsed) — no reason is
// recorded, closing without incident needs no paperwork. Reopening restores the status the
// package's data implies (quotes → QuotesReceived, recipients → Inviting, else Draft).
public enum BidPackageStatus
{
    Draft = 0,
    Inviting = 1,
    QuotesReceived = 2,
    Awarded = 4,
    Closed = 5
}

// Where a single invited subcontractor sits on a bid package. Invited is the default once an invite
// is issued; Responded when their tender comes back; Declined if they opt out; Won when awarded.
public enum BidPackageRecipientStatus
{
    Invited = 0,
    Responded = 1,
    Declined = 2,
    Won = 3
}

// A subcontractor invited to tender for a bid package — the "who was invited and when" the Bid
// Package Invites tab shows. One row per (package, subcontractor).
public sealed record BidPackageRecipient(
    string RecipientId,
    string BidPackageId,
    string SubcontractorId,
    BidPackageRecipientStatus Status,
    DateTimeOffset InvitedAt,
    DateTimeOffset? RespondedAt = null);

// What a bid package line item is covered by — the commercial home the tendered scope maps to. A line
// is either backed by a contract BoQ line (so it flows into the Programme Valuation Report against the
// original tender) OR by a Variation Order (extra-over scope priced outside the contract sum) —
// never both. Unassigned until the QS links it.
public enum BidPackageLineCoverage
{
    Unassigned = 0,  // not yet linked to a commercial home
    ContractLine = 1, // backed by a contract BoQ line (BoqLineItemId set) — shows in the Valuation Report
    Variation = 2    // backed by a Variation Order (VariationOrderId set)
}

// A priced line on a bid package — the scope items a subcontractor tenders against. Grouped in the UI
// by Trade/speciality (e.g. electrician, plumber). Quantity + Unit describe the work; pricing is
// captured per response, not here. CostCode is the cost centre (from the master list) the line's
// committed value lands on — required for every line put out to tender, so the cost-centre home is
// known before a work order is ever raised (empty only on legacy rows that predate the rule).
// Coverage links the line to exactly one commercial home: a contract BoQ line (ContractLine +
// BoqLineItemId) or a Variation Order (Variation + VariationOrderId).
public sealed record BidPackageLineItem(
    string LineItemId,
    string BidPackageId,
    string Description,
    string Unit,
    decimal Quantity,
    string Trade,
    string CostCode,
    int SortOrder,
    BidPackageLineCoverage Coverage = BidPackageLineCoverage.Unassigned,
    string? BoqLineItemId = null,
    string? VariationOrderId = null);

public sealed record BidPackage(
    string BidPackageId,
    string ProjectId,
    string Title,
    string Trade,
    BidPackageStatus Status,
    DateTimeOffset CreatedAt,
    string OwnerEmail,
    string? VariationOrderId = null,        // LEGACY parent VO (pre-2026-08-12, when packages could be
                                             // created under a variation). Never set on new packages;
                                             // line-item Coverage is how scope maps to a variation now
    int Number = 0,                          // sequential; rendered BPI-0001 via Reference
    bool MaterialsApplicable = false,        // materials matter to this scope — the tender invite asks
                                             // whether the subcontractor will supply their own
    DateTimeOffset? ClosedAt = null,         // when the package was closed without a winner; null
                                             // unless Status is Closed (cleared on reopen)
    string SpecificationSummary = "")        // the "what this package covers" bullets printed at the
                                             // top of the pricing schedule workbook; optional
{
    // Human, collision-safe reference and the stem tagged on the package's emails ("JPMS/BPI-0001"),
    // so RFT responses group under the package in the Bid Package Invites section. Blank until numbered.
    public string Reference => Number > 0 ? $"BPI-{Number:0000}" : "";
}

public sealed record Quote(
    string QuoteId,
    string BidPackageId,
    string SubcontractorId,
    decimal Value,
    string Notes,
    DateTimeOffset ReceivedAt,
    bool IsDeclined);

// A priced line on a subcontractor's quote — their rate against one of the package's line items.
// BidPackageLineItemId links the quoted line to the package line it prices (null when the subbie
// quoted something outside the package's scope, e.g. an attendance or a lump-sum extra); the
// comparison view aligns quotes side by side through that link. Total is Quantity × Rate as the
// subcontractor stated it (kept verbatim rather than recomputed, so a lump-sum line survives).
public sealed record QuoteLineItem(
    string QuoteLineItemId,
    string QuoteId,
    string? BidPackageLineItemId,
    string Description,
    string Unit,
    decimal Quantity,
    decimal Rate,
    decimal Total);

// Where a work order sits in its lifecycle. Draft while being put together — it counts in the
// Financials tab's committed figures (the commitment is intended) but is invisible to the
// supplier and can't be invoiced, packaged or tagged until approved. Approving mints the next
// sequential number and lands here as Released (the persisted name predates the approve verb;
// Buildertrend's "Date Released"); Rejected is the terminal answer to a draft that shouldn't
// proceed — it counts nowhere, like Cancelled. Complete when the works are done and settled;
// Cancelled if a live order is withdrawn. Awarding a bid package creates the order Released.
public enum WorkOrderStatus
{
    Draft = 0,
    Released = 1,
    Complete = 2,
    Cancelled = 3,
    Rejected = 4
}

// The purchase-order record raised against a supplier — the business calls these work orders.
// BidPackageId is set when the order came from awarding a tender; null for orders raised directly
// or seeded from Buildertrend. Value is the order total; the per-cost-code detail lives on the
// order's WorkOrderLines.
public sealed record WorkOrder(
    string WorkOrderId,
    string ProjectId,
    string? BidPackageId,
    string SubcontractorId,
    decimal Value,
    string Scope,
    DateTimeOffset AwardedAt,
    string AwardedByEmail,
    int Number,
    string Title,
    WorkOrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledCompletion,
    // Set when this order was issued to instruct an approved variation order (never an uplift of
    // an existing order -- variations always produce a new WO).
    string? VariationOrderId = null,
    // External id of the record this order was seeded from (e.g. the Buildertrend PO id).
    // Null for orders raised in JPMS.
    string? SourceReference = null,
    // ---- Programme (printed on the purchase order's Programme section when any is set;
    //      ScheduledCompletion above is the target completion date) ----
    DateTimeOffset? ProgrammeStart = null,
    string ProgrammeNotes = "",
    // ---- Electronic acceptance from the subcontractor portal (one click under their login) ----
    DateTimeOffset? AcceptedAt = null,
    string AcceptedByEmail = "",
    string AcceptedByName = "",
    // ---- Deposit the supplier requires, printed at the foot of the purchase order.
    //      Recorded as a percentage of the order value only; Percent is null unless required ----
    bool DepositRequired = false,
    decimal? DepositPercent = null)
{
    /// <summary>The supplier has electronically accepted this order from the portal.</summary>
    public bool IsAccepted => AcceptedAt is not null;

    /// <summary>Still being put together — unnumbered and invisible to the supplier, though it
    /// already counts in committed figures. ApproveWorkOrder mints the sequential number and
    /// makes it a live order; RejectWorkOrder ends it.</summary>
    public bool IsDraft => Status == WorkOrderStatus.Draft;

    /// <summary>A rejected draft: terminal, unnumbered, counts nowhere.</summary>
    public bool IsRejected => Status == WorkOrderStatus.Rejected;

    /// <summary>A cancelled order: terminal, like a rejected draft — but it was ISSUED first,
    /// so it keeps its minted number and stays visible as the record of a voided purchase
    /// order. Its value counts nowhere (CancelWorkOrder refuses while bills or paid money
    /// are recorded against it, so there is never a cost left behind to account for).</summary>
    public bool IsCancelled => Status == WorkOrderStatus.Cancelled;

    /// <summary>The human reference. Drafts and rejected drafts have no number — approval is
    /// what mints it — so they read by their state; anything else unnumbered falls back to an
    /// id stem, mirroring WorkOrderEntity.Reference server-side. Never render
    /// WO-{Number:0000} directly: a draft's 0 would print as "WO-0000".</summary>
    public string Reference => Number > 0
        ? $"WO-{Number:0000}"
        : IsDraft
            ? "Draft"
            : IsRejected
                ? "Rejected"
                : "WO-" + (WorkOrderId.Length >= 8 ? WorkOrderId[..8] : WorkOrderId).ToUpperInvariant();

    /// <summary>Raised directly in JPMS — no tender, no variation, no seed — so its supplier,
    /// title, scope and priced lines can be edited wholesale via UpdateManualWorkOrder.</summary>
    public bool IsManual => BidPackageId is null && VariationOrderId is null && SourceReference is null;
}

// A priced line on a work order. Each line carries its own cost centre code (CostCode, from the
// current master list) — cost-centre totals aggregate lines, not orders. PaidToDate is what has
// been paid against the line so far.
public sealed record WorkOrderLine(
    string WorkOrderLineId,
    string WorkOrderId,
    string Title,
    string Description,
    string CostType,
    string CostCode,
    decimal Quantity,
    string Unit,
    decimal UnitCost,
    decimal LineTotal,
    decimal PaidToDate,
    int SortOrder);

// A work order with everything the Work Orders tab renders: the header, the supplier's display
// name (resolved so the tab doesn't join the directory client-side), and the priced lines whose
// cost codes drive the tab's cost-centre grouping.
public sealed record ProjectWorkOrderDetail(
    WorkOrder Order,
    string SubcontractorName,
    IReadOnlyList<WorkOrderLine> Lines);
