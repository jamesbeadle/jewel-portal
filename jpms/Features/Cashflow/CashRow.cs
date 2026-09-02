namespace Jewel.JPMS.Features.Cashflow;

/// <summary>One project's to-completion statement collapsed to a line — computed by the page
/// exactly as the project's Cashflow tab computes it, same sources, same helpers, so the two can
/// never disagree; summed across the selection for the combined statement.</summary>
public sealed record CashRow(
    decimal ProjectClaim,
    decimal CashReceived,
    decimal RetentionOutstanding,
    decimal InvoicedAwaitingPayment,
    decimal RetentionStillToWithhold,
    decimal LeftToClaim,
    decimal Drawdown,
    decimal WoCommitted,
    decimal WoInvoiced,
    decimal BillsUnpaid,
    decimal Release1,
    decimal Release2)
{
    public decimal CashAllocated => CashReceived + RetentionOutstanding;

    public decimal WoLeftToInvoice => WoCommitted - WoInvoiced;

    public decimal PracticalCompletionCashflow =>
        LeftToClaim - Drawdown - WoLeftToInvoice - BillsUnpaid + Release1;

    public decimal ProjectCompletionCashflow => PracticalCompletionCashflow + Release2;
}
