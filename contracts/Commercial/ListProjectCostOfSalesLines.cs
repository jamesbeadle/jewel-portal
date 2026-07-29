using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Every allocated Xero purchase line on the project — the WO Allocation tab's queue.
/// Whole-line allocations carry their centre and can be linked to a work order; a
/// split line appears once per share (IsSplit true, Net = the share, one row per
/// centre) and can't be linked — linking classifies the whole ledger line. Net is
/// sign-adjusted: supplier credit notes (ACCPAYCREDIT) come back negative. Newest first.
/// </summary>
public sealed record ListProjectCostOfSalesLines(string ProjectId) : IQuery<IReadOnlyList<ProjectCostOfSalesLine>>;

public sealed record ProjectCostOfSalesLine(
    string XeroLedgerLineId,
    DateTime? Date,
    string Supplier,
    string InvoiceNumber,
    string Description,
    string CostCode,
    decimal Net,
    bool IsSplit,
    IReadOnlyList<XeroWorkOrderLinkSlice>? WorkOrderLinks = null, // the order(s) this line pays against, with each one's share
    // Xero's invoice status as last synced (PAID, AUTHORISED, SUBMITTED, DRAFT …) —
    // whether the supplier's bill has actually been settled, for cash-position views.
    string InvoiceStatus = "",
    // The bill's payment state, carried per line exactly as XeroLedgerLines stores it:
    // Tax is this row's VAT share (sign-adjusted with Net; split shares pro-rata),
    // InvoiceTotal the bill's gross as Xero states it, AmountDue what is still
    // outstanding on the bill. Both totals read 0 on rows synced before the
    // AddXeroLinePaymentState migration — the derived members below fall back to
    // InvoiceStatus for those until the next ledger sync fills them.
    decimal Tax = 0m,
    decimal InvoiceTotal = 0m,
    decimal AmountDue = 0m)
{
    // ── Payment position, part-payment aware (XeroPaymentMaths: CIS-safe, driven off
    // AmountDue so a CIS deduction never reads as unpaid). ──
    public decimal SettledFraction => XeroPaymentMaths.SettledFraction(InvoiceStatus, InvoiceTotal, AmountDue);

    /// <summary>Nothing further is owed on this line's bill — settled in full (or, for
    /// rows without synced amounts, Xero's PAID status). Same meaning the old
    /// status-string test carried, now part-payment aware.</summary>
    public bool IsPaid => SettledFraction == 1m;

    /// <summary>The settled part of this row's net, to the penny.</summary>
    public decimal PaidNet => XeroPaymentMaths.PaidPartOfSlice(Net, InvoiceStatus, InvoiceTotal, AmountDue);

    /// <summary>The net still owed on this row — a part-paid bill contributes only its
    /// remainder, a settled one 0. This is what cash-position views sum.</summary>
    public decimal OutstandingNet => Net - PaidNet;

    /// <summary>This row's share of the bill's gross (net + VAT).</summary>
    public decimal Gross => Net + Tax;

    /// <summary>The gross still owed on this row — the figure that ties line-for-line to
    /// Xero's Aged Payables Detail (to within Xero's per-line VAT rounding).</summary>
    public decimal OutstandingGross =>
        SettledFraction == 0m ? Gross
        : SettledFraction == 1m ? 0m
        : Math.Round(Gross * (1m - SettledFraction), 2, MidpointRounding.AwayFromZero);

    public IReadOnlyList<XeroWorkOrderLinkSlice> Links => WorkOrderLinks ?? Array.Empty<XeroWorkOrderLinkSlice>();
    public decimal LinkedTotal => Links.Sum(link => link.Amount);
    // The share of the line not yet paying any work order — non-work-order cost of
    // sales. Split shares can't carry links, so their whole net is unlinked.
    public decimal UnlinkedRemainder => Net - LinkedTotal;
}
