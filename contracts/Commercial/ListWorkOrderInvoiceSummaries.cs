using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Invoicing progress for every work order on the project: the order's value, the
/// signed net of the Xero purchase lines linked to it (credit notes subtract), and
/// what is left to be invoiced. The single source of truth behind the WO Allocation
/// tab, the Work Orders tab's invoiced figures, and the Financials modal's
/// remaining-balance labels. Ordered by order number.
/// </summary>
public sealed record ListWorkOrderInvoiceSummaries(string ProjectId) : IQuery<IReadOnlyList<WorkOrderInvoiceSummary>>;

/// <summary>
/// Where an order sits against its value. OverInvoiced can only describe links made
/// before the balance check existed — the link command now refuses an allocation
/// that would pass the order's remaining balance.
/// </summary>
public enum WorkOrderInvoicingStatus
{
    NotInvoiced = 0,
    PartInvoiced = 1,
    FullyInvoiced = 2,
    OverInvoiced = 3
}

public sealed record WorkOrderInvoiceSummary(
    string WorkOrderId,
    int Number,
    string Title,
    string SubcontractorName,
    WorkOrderStatus Status,
    decimal Value,
    decimal InvoicedToDate,
    decimal RemainingToInvoice,
    int LinkedLineCount,
    WorkOrderInvoicingStatus InvoicingStatus);
