namespace Jewel.JPMS.Models;

public enum BidPackageStatus
{
    Draft,
    Inviting,
    QuotesReceived,
    Comparing,
    Awarded
}

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
