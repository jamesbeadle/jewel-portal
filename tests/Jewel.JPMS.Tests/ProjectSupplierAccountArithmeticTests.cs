using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The supplier account's arithmetic, pinned on S Williams at By France — the case that asked for
/// it (2026-09-14): three invoices totalling £74,172.64 against orders of £72,702.67, the third
/// still awaiting approval and linked to nothing, so £1,469.97 over the orders.
/// </summary>
public class ProjectSupplierAccountArithmeticTests
{
    private const string ByFrance = "project-by-france";

    [Fact]
    public void Account_addsUpTheAccountantsPosition()
    {
        var account = SWilliamsAtByFrance();

        Assert.Equal(72_702.67m, account.Ordered);
        Assert.Equal(52_460.27m, account.InvoicedAndLinked);
        Assert.Equal(20_242.40m, account.LeftToInvoice);
        Assert.Equal(52_460.27m, account.Paid);
        Assert.Equal(46_850.00m, account.LabourReceived);
        Assert.Equal(27_322.64m, account.MaterialsReceived);
        Assert.Equal(74_172.64m, account.Received);
        Assert.Equal(21_712.37m, account.Unmatched);
        Assert.Equal(1_469.97m, account.OverOrders);
        Assert.True(account.IsOverInvoiced);
        Assert.True(account.HasInvoicesAwaitingApproval);

        var pending = account.Invoices.Single(invoice => invoice.InvoiceNumber == "027259");
        Assert.Equal(ProjectSupplierInvoiceState.AwaitingApproval, pending.State);
        Assert.Equal(0m, pending.SettledNet);
        Assert.False(pending.IsMatched);
        Assert.Equal(21_712.37m, pending.Unmatched);

        var mainOrder = account.Orders.Single(order => order.Number == 24);
        Assert.Equal(18_882.49m, mainOrder.RemainingToInvoice);
        Assert.Equal(new[] { "027021", "027093" }, mainOrder.Invoices.Select(invoice => invoice.InvoiceNumber));
    }

    private static ProjectSupplierAccount SWilliamsAtByFrance()
    {
        var mainOrderShares = new[] { new ProjectSupplierAccountOrderShare("wo-24", "WO-0024", 18_611.66m) };
        var secondShares = new[] { new ProjectSupplierAccountOrderShare("wo-24", "WO-0024", 33_848.61m) };
        var invoices = new[]
        {
            Invoice("027021", new DateTime(2026, 2, 18), 6_000.00m, 12_611.66m, "PAID", amountDue: 0m, mainOrderShares),
            Invoice("027093", new DateTime(2026, 4, 27), 24_100.00m, 9_748.61m, "PAID", amountDue: 0m, secondShares),
            Invoice("027259", new DateTime(2026, 9, 9), 16_750.00m, 4_962.37m, "DRAFT", amountDue: 21_712.37m, Array.Empty<ProjectSupplierAccountOrderShare>())
        };
        var orders = new[]
        {
            Order("wo-24", 24, 71_342.76m, invoicedToDate: 52_460.27m, paidToDate: 52_460.27m, WorkOrderPaymentStatus.PartPaid,
                new ProjectSupplierAccountInvoiceShare("inv-027021", "027021", 18_611.66m),
                new ProjectSupplierAccountInvoiceShare("inv-027093", "027093", 33_848.61m)),
            Order("wo-41", 41, 1_359.91m, invoicedToDate: 0m, paidToDate: 0m, WorkOrderPaymentStatus.NotLinked)
        };
        return new ProjectSupplierAccount(ByFrance, "JBB-2026-001", "By France", "sub-sw", "S Williams Plumbing & Heating",
            new DateTimeOffset(2026, 9, 14, 14, 28, 0, TimeSpan.Zero), null, orders, invoices);
    }

    private static ProjectSupplierAccountOrder Order(
        string id, int number, decimal value, decimal invoicedToDate, decimal paidToDate, WorkOrderPaymentStatus paymentStatus,
        params ProjectSupplierAccountInvoiceShare[] invoices) =>
        new(id, number, $"WO-{number:0000}", "Plumbing and heating", WorkOrderStatus.Released,
            new DateTimeOffset(2025, 9, 19, 0, 0, 0, TimeSpan.Zero), value, Array.Empty<ProjectSupplierAccountOrderLine>(),
            invoicedToDate, paidToDate, paymentStatus, invoices);

    private static ProjectSupplierAccountInvoice Invoice(
        string number, DateTime date, decimal labour, decimal materials, string xeroStatus, decimal amountDue,
        IReadOnlyList<ProjectSupplierAccountOrderShare> orders) =>
        new($"inv-{number}", number, null, date, IsCreditNote: false, labour, materials, NetElsewhere: 0m,
            xeroStatus, InvoiceTotal: labour + materials, amountDue, ProjectSupplierInvoicePlacement.Allocated, orders);
}
