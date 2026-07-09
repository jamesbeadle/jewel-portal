using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Ties an allocated Xero purchase line to the work order it pays against, from the
/// Financials tab's cost-of-sales detail. WorkOrderId null clears the link. Lines
/// without a link count as non-work-order cost of sales, which draws down the cost
/// centre's target cost alongside its work orders.
/// </summary>
public sealed record LinkXeroLineToWorkOrder(
    string ProjectId,
    string XeroLedgerLineId,
    string? WorkOrderId) : ICommand<Acknowledgement>;
