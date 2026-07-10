using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// One work order's share of a Xero purchase line. Amount is signed the same way as
/// the line's net (credit notes negative) and is the slice's contribution toward the
/// order's invoiced-to-date figure.
/// </summary>
public sealed record XeroWorkOrderLinkSlice(string WorkOrderId, decimal Amount);

/// <summary>
/// Replaces the set of work-order links on an allocated Xero purchase line. One slice
/// covering the full net is the everyday whole-line link; several slices split a bill
/// that pays multiple orders at once (e.g. a subcontractor invoicing a main order plus
/// two small variation orders on one bill). An empty list clears all links. The slices
/// may total less than the line — the unallocated remainder counts as non-work-order
/// cost of sales — but never more, and no slice may take its order past its value.
/// </summary>
public sealed record SetXeroLineWorkOrderLinks(
    string ProjectId,
    string XeroLedgerLineId,
    IReadOnlyList<XeroWorkOrderLinkSlice> Links) : ICommand<Acknowledgement>;
