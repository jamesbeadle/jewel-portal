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

// A priced line on a bid package — the scope items a subcontractor tenders against. Grouped in the UI
// by Trade/speciality (e.g. electrician, plumber). Quantity + Unit describe the work; pricing is
// captured per response, not here.
public sealed record BidPackageLineItem(
    string LineItemId,
    string BidPackageId,
    string Description,
    string Unit,
    decimal Quantity,
    string Trade,
    int SortOrder);

public sealed record BidPackage(
    string BidPackageId,
    string ProjectId,
    string Title,
    string Trade,
    BidPackageStatus Status,
    DateTimeOffset CreatedAt,
    string OwnerEmail);

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
