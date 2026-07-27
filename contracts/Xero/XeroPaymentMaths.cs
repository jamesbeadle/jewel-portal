namespace Jewel.JPMS.Contracts.Xero;

/// <summary>
/// How much of a bill Xero has actually settled, as a fraction of the bill's own total —
/// the step that turns "this purchase line is linked to WO-0018" into "WO-0018 has been paid".
///
/// Driven off AmountDue rather than AmountPaid on purpose, because of CIS. A CIS
/// subcontractor's GBP 800 bill is settled by a GBP 720 payment plus a GBP 80 deduction
/// withheld and paid over to HMRC: AmountPaid reads 720, and scaling by it would leave every
/// CIS supplier permanently 20% short of their order value and no order would ever close out.
/// AmountDue reaching zero is the honest "nothing further is owed on this bill" signal
/// whatever the deduction. So the FRACTION is what has stopped being owed, and the VALUE it
/// scales is the work order's own net slice — never Xero's cash figure.
///
/// An InvoiceTotal of zero means the amount fields have not been synced onto this line yet:
/// they arrived with the AddXeroLinePaymentState migration and are filled by the next ledger
/// sync. The invoice status is the fallback until then — PAID is all of it, anything else none.
/// </summary>
public static class XeroPaymentMaths
{
    /// <summary>Xero's terminal "nothing outstanding" status for a bill or credit note.</summary>
    public static bool IsSettledStatus(string? invoiceStatus) =>
        string.Equals(invoiceStatus, "PAID", StringComparison.OrdinalIgnoreCase);

    /// <summary>0 = nothing settled, 1 = settled in full, in between for a part payment.</summary>
    public static decimal SettledFraction(string? invoiceStatus, decimal invoiceTotal, decimal amountDue)
    {
        if (invoiceTotal <= 0m) return IsSettledStatus(invoiceStatus) ? 1m : 0m;

        var settled = (invoiceTotal - amountDue) / invoiceTotal;
        return settled <= 0m ? 0m
            : settled >= 1m ? 1m
            : settled;
    }

    /// <summary>
    /// The settled part of one work-order link slice, to the penny. The slice carries the
    /// ledger line's own sign, so an applied credit note subtracts from the order's paid
    /// position exactly as it already subtracts from its invoiced position. A fully settled
    /// bill returns the slice untouched rather than a rounded product, so the everyday case
    /// can never drift a penny from the order value.
    /// </summary>
    public static decimal PaidPartOfSlice(
        decimal sliceAmount, string? invoiceStatus, decimal invoiceTotal, decimal amountDue)
    {
        var fraction = SettledFraction(invoiceStatus, invoiceTotal, amountDue);
        return fraction == 1m ? sliceAmount
            : fraction == 0m ? 0m
            : Math.Round(sliceAmount * fraction, 2, MidpointRounding.AwayFromZero);
    }
}
