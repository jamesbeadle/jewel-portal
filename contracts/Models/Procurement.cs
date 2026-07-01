namespace Jewel.JPMS.Models;

public enum BidPackageStatus
{
    Draft,
    Inviting,
    QuotesReceived,
    Comparing,
    Awarded
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
// original tender) OR by a Variation Order Quote (extra-over scope priced outside the contract sum) —
// never both. Unassigned until the QS links it.
public enum BidPackageLineCoverage
{
    Unassigned = 0,  // not yet linked to a commercial home
    ContractLine = 1, // backed by a contract BoQ line (BoqLineItemId set) — shows in the Valuation Report
    Variation = 2    // backed by a Variation Order Quote (VariationOrderQuoteId set)
}

// A priced line on a bid package — the scope items a subcontractor tenders against. Grouped in the UI
// by Trade/speciality (e.g. electrician, plumber). Quantity + Unit describe the work; pricing is
// captured per response, not here. Coverage links the line to exactly one commercial home: a contract
// BoQ line (ContractLine + BoqLineItemId) or a Variation Order Quote (Variation + VariationOrderQuoteId).
public sealed record BidPackageLineItem(
    string LineItemId,
    string BidPackageId,
    string Description,
    string Unit,
    decimal Quantity,
    string Trade,
    int SortOrder,
    BidPackageLineCoverage Coverage = BidPackageLineCoverage.Unassigned,
    string? BoqLineItemId = null,
    string? VariationOrderQuoteId = null);

public sealed record BidPackage(
    string BidPackageId,
    string ProjectId,
    string Title,
    string Trade,
    BidPackageStatus Status,
    DateTimeOffset CreatedAt,
    string OwnerEmail,
    string? VariationOrderQuoteId = null,   // parent VOQ, when this package belongs to one
    int Number = 0)                          // sequential; rendered BPI-0001 via Reference
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

public sealed record WorkOrder(
    string WorkOrderId,
    string ProjectId,
    string BidPackageId,
    string SubcontractorId,
    decimal Value,
    string Scope,
    DateTimeOffset AwardedAt,
    string AwardedByEmail);
