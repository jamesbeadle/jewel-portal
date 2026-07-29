using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Unpaid Xero purchase lines whose Sites tracking matches this project but which nobody
/// has allocated in JPMS yet — money Xero holds against the site that no project view is
/// counting. The guard behind the Cashflow tab's unallocated warning: deliberately
/// read-only and never auto-allocated (allocation is a human decision, made on the Xero
/// allocation screen), it just makes the queue visible where the money is missed.
/// </summary>
public sealed record ListUnallocatedSiteBills(string ProjectId) : IQuery<IReadOnlyList<UnallocatedSiteBill>>;

public sealed record UnallocatedSiteBill(
    string XeroLedgerLineId,
    DateTime? Date,
    string Supplier,
    string InvoiceNumber,
    string Description,
    // Sign-adjusted like the cost-of-sales queue: credit notes come back negative.
    decimal Net,
    decimal Tax,
    string InvoiceStatus,
    decimal InvoiceTotal,
    decimal AmountDue)
{
    public decimal SettledFraction => XeroPaymentMaths.SettledFraction(InvoiceStatus, InvoiceTotal, AmountDue);
    public decimal OutstandingNet => Net - XeroPaymentMaths.PaidPartOfSlice(Net, InvoiceStatus, InvoiceTotal, AmountDue);
    public decimal Gross => Net + Tax;
    public decimal OutstandingGross =>
        SettledFraction == 0m ? Gross
        : SettledFraction == 1m ? 0m
        : Math.Round(Gross * (1m - SettledFraction), 2, MidpointRounding.AwayFromZero);
}
