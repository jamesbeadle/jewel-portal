using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Xero;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// Turning a linked Xero bill into a work order's paid position. The figure that matters is
/// the ORDER's own net slice, scaled by how much of the bill has stopped being owed — never
/// Xero's cash figure, which under CIS is 20% short of what settles the order.
/// </summary>
public class XeroPaymentMathsTests
{
    [Fact]
    public void SettledBillPaysItsSliceInFull()
    {
        Assert.Equal(800m, XeroPaymentMaths.PaidPartOfSlice(800m, "PAID", invoiceTotal: 800m, amountDue: 0m));
    }

    [Fact]
    public void CisDeductionDoesNotLeaveTheOrderShort()
    {
        // Mega Scaffolding 3582/2910/25: GBP 800 net, domestic reverse charge so no VAT,
        // settled by a GBP 720 payment plus GBP 80 CIS withheld. AmountDue is 0, so the
        // GBP 800 work order is paid in full — scaling by the GBP 720 cash figure would
        // strand GBP 80 against it for ever.
        Assert.Equal(800m, XeroPaymentMaths.PaidPartOfSlice(800m, "PAID", invoiceTotal: 800m, amountDue: 0m));
        Assert.NotEqual(720m, XeroPaymentMaths.PaidPartOfSlice(800m, "PAID", invoiceTotal: 800m, amountDue: 0m));
    }

    [Fact]
    public void AuthorisedButUnpaidBillPaysNothing()
    {
        Assert.Equal(0m, XeroPaymentMaths.PaidPartOfSlice(2100m, "AUTHORISED", invoiceTotal: 2100m, amountDue: 2100m));
    }

    [Fact]
    public void PartPaymentPaysTheSameProportionOfTheSlice()
    {
        // 600 of a 1,000 bill settled -> 60% of the order's slice.
        Assert.Equal(600m, XeroPaymentMaths.PaidPartOfSlice(1000m, "AUTHORISED", invoiceTotal: 1000m, amountDue: 400m));
    }

    [Fact]
    public void PartPaymentRoundsToThePenny()
    {
        // Two thirds of 100.00.
        Assert.Equal(66.67m, XeroPaymentMaths.PaidPartOfSlice(100m, "AUTHORISED", invoiceTotal: 3m, amountDue: 1m));
    }

    [Fact]
    public void UnsyncedAmountsFallBackToTheInvoiceStatus()
    {
        // Rows written before AddXeroLinePaymentState carry InvoiceTotal 0 until the next
        // ledger sync — the status has to carry them, and it does, so the fix is live on
        // deploy rather than on the next sync.
        Assert.Equal(500m, XeroPaymentMaths.PaidPartOfSlice(500m, "PAID", invoiceTotal: 0m, amountDue: 0m));
        Assert.Equal(0m, XeroPaymentMaths.PaidPartOfSlice(500m, "AUTHORISED", invoiceTotal: 0m, amountDue: 0m));
        Assert.Equal(0m, XeroPaymentMaths.PaidPartOfSlice(500m, "DRAFT", invoiceTotal: 0m, amountDue: 0m));
    }

    [Fact]
    public void AppliedCreditNoteSubtractsFromThePaidPosition()
    {
        // Credit-note slices are negative, exactly as they are on the invoiced side.
        Assert.Equal(-250m, XeroPaymentMaths.PaidPartOfSlice(-250m, "PAID", invoiceTotal: 250m, amountDue: 0m));
    }

    [Fact]
    public void UnappliedCreditNoteChangesNothing()
    {
        Assert.Equal(0m, XeroPaymentMaths.PaidPartOfSlice(-250m, "AUTHORISED", invoiceTotal: 250m, amountDue: 250m));
    }

    [Fact]
    public void OverpaymentNeverPaysMoreThanTheSlice()
    {
        // A negative AmountDue (Xero holding an overpayment against the bill) must not push
        // the order past its own value.
        Assert.Equal(800m, XeroPaymentMaths.PaidPartOfSlice(800m, "PAID", invoiceTotal: 800m, amountDue: -50m));
    }

    [Fact]
    public void SettledFractionIsClampedBothWays()
    {
        Assert.Equal(0m, XeroPaymentMaths.SettledFraction("AUTHORISED", 100m, 250m));
        Assert.Equal(1m, XeroPaymentMaths.SettledFraction("PAID", 100m, -10m));
    }

    [Fact]
    public void SlicesOfOneBillSumToWhatTheBillSettled()
    {
        // A bill split across a main order and its variation: the parts must total the
        // settled value of the whole, or the project's paid position drifts.
        var main = XeroPaymentMaths.PaidPartOfSlice(700m, "AUTHORISED", invoiceTotal: 1000m, amountDue: 500m);
        var variation = XeroPaymentMaths.PaidPartOfSlice(300m, "AUTHORISED", invoiceTotal: 1000m, amountDue: 500m);
        Assert.Equal(500m, main + variation);
    }
}

/// <summary>
/// The zero that lies. "GBP 0.00 paid" and "we have not been told what is paid" are different
/// answers, and an order with no bill linked to it must never be able to pass for an order
/// whose bills are linked and unpaid.
/// </summary>
public class WorkOrderPaymentStatusTests
{
    [Fact]
    public void NothingLinkedAndNothingCarriedOverIsNotLinked()
    {
        // Cedar Views: no bill linked, no opening balance. The tab shows an em dash, not GBP 0.00.
        Assert.Equal(WorkOrderPaymentStatus.NotLinked,
            WorkOrderPaymentStatuses.For(0, 0m, 3777.60m));
    }

    [Fact]
    public void NothingLinkedButCarriedOverIsAnOpeningBalance()
    {
        // A migrated order on a project nobody has linked up yet: there IS a figure, but Xero
        // has confirmed none of it.
        Assert.Equal(WorkOrderPaymentStatus.OpeningBalance,
            WorkOrderPaymentStatuses.For(0, 24569.42m, 40949.03m));
    }

    [Fact]
    public void LinkedAndUnsettledIsUnpaid()
    {
        // NJW Scaffolding: bill linked, Xero has not paid it. This one IS an honest zero.
        Assert.Equal(WorkOrderPaymentStatus.Unpaid,
            WorkOrderPaymentStatuses.For(1, 0m, 2100m));
    }

    [Fact]
    public void LinkedAndSettledInFullIsPaid()
    {
        // Mega Scaffolding WO-0018 after the fix.
        Assert.Equal(WorkOrderPaymentStatus.Paid,
            WorkOrderPaymentStatuses.For(1, 800m, 800m));
    }

    [Fact]
    public void LinkedAndPartlySettledIsPartPaid()
    {
        Assert.Equal(WorkOrderPaymentStatus.PartPaid,
            WorkOrderPaymentStatuses.For(1, 24569.42m, 40949.03m));
    }

    [Fact]
    public void CreditNotesTakingThePositionNegativeReadAsUnpaid()
    {
        Assert.Equal(WorkOrderPaymentStatus.Unpaid,
            WorkOrderPaymentStatuses.For(2, -50m, 800m));
    }

    [Fact]
    public void ZeroValueOrderWithSomethingPaidIsNotCalledPaid()
    {
        // Guard against a divide-by-nothing reading as "settled in full".
        Assert.Equal(WorkOrderPaymentStatus.PartPaid,
            WorkOrderPaymentStatuses.For(1, 130m, 0m));
    }
}
