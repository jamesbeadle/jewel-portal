using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Models;

/// <summary>
/// One live order (Released or Complete) the supplier holds on the project, with the priced lines
/// it was issued for and the invoices matched to it. InvoicedToDate is the signed sum of the
/// invoice shares below, so the order's remaining balance always reconciles with the invoice list
/// — the WO Allocation tab's figure, read from the supplier's side.
/// </summary>
public sealed record ProjectSupplierAccountOrder(
    string WorkOrderId,
    int Number,
    string Reference,
    string Title,
    WorkOrderStatus Status,
    DateTimeOffset AwardedAt,
    decimal Value,
    IReadOnlyList<ProjectSupplierAccountOrderLine> Lines,
    decimal InvoicedToDate,
    // What Xero has settled of the linked invoices — or the migrated opening balance when nothing
    // is linked. Read PaymentStatus first: NotLinked means nothing is known, not that nothing has
    // been paid, and the reader is shown a dash rather than this figure.
    decimal PaidToDate,
    WorkOrderPaymentStatus PaymentStatus,
    IReadOnlyList<ProjectSupplierAccountInvoiceShare> Invoices)
{
    public decimal RemainingToInvoice => Value - InvoicedToDate;

    public bool IsPaymentKnown => PaymentStatus != WorkOrderPaymentStatus.NotLinked;
}

/// <summary>One priced line of the order — the variation lines listed under an order.</summary>
public sealed record ProjectSupplierAccountOrderLine(string Title, string CostCode, decimal LineTotal);

/// <summary>An invoice's share against an order — one row per invoice, its slices summed.</summary>
public sealed record ProjectSupplierAccountInvoiceShare(string XeroInvoiceId, string InvoiceNumber, decimal Amount);

/// <summary>An order's share of an invoice — one row per order, its slices summed.</summary>
public sealed record ProjectSupplierAccountOrderShare(string WorkOrderId, string Reference, decimal Amount);
