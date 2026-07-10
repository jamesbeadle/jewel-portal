using Jewel.JPMS.Commercial;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Contract-level guarantees for the valuation-invoice approval workflow and the
// report-snapshot model (docs/Valuation-Invoice-Approval-Snapshot-Spec.md). The
// handler-side transition rules live in the API; these tests pin down the parts the
// whole system leans on: stable enum numbering, the pending/editable state groupings,
// and the certified-to-date arithmetic that manual (historic) invoices feed.
public sealed class ValuationInvoiceLifecycleTests
{
    private static ValuationInvoice Invoice(ValuationInvoiceStatus status, bool isManual = false, decimal amount = 1_000m, decimal amountPaid = 0m) =>
        new(
            ValuationInvoiceId: "VI1",
            ProjectId: "PRJ-1",
            ValuationClaimId: null,
            Number: 7,
            Reference: "VI-0007",
            PeriodMonth: new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero),
            Amount: amount,
            AmountPaid: amountPaid,
            Status: status,
            RaisedAt: DateTimeOffset.UtcNow,
            IsManual: isManual);

    // The first three statuses predate the approval workflow and are persisted as ints
    // (and seeded); the approval states were appended. Renumbering would corrupt data.
    [Fact]
    public void StatusEnum_intValuesAreStable()
    {
        Assert.Equal(0, (int)ValuationInvoiceStatus.Raised);
        Assert.Equal(1, (int)ValuationInvoiceStatus.Issued);
        Assert.Equal(2, (int)ValuationInvoiceStatus.Paid);
        Assert.Equal(3, (int)ValuationInvoiceStatus.Submitted);
        Assert.Equal(4, (int)ValuationInvoiceStatus.Approved);
        Assert.Equal(5, (int)ValuationInvoiceStatus.Rejected);
        Assert.Equal(6, (int)ValuationInvoiceStatus.Cancelled);
    }

    [Theory]
    [InlineData(ValuationInvoiceStatus.Submitted, true)]
    [InlineData(ValuationInvoiceStatus.Approved, true)]
    [InlineData(ValuationInvoiceStatus.Raised, false)]
    [InlineData(ValuationInvoiceStatus.Rejected, false)]
    [InlineData(ValuationInvoiceStatus.Issued, false)]
    [InlineData(ValuationInvoiceStatus.Paid, false)]
    [InlineData(ValuationInvoiceStatus.Cancelled, false)]
    public void IsAwaitingApproval_isSubmittedOrApprovedOnly(ValuationInvoiceStatus status, bool expected) =>
        Assert.Equal(expected, Invoice(status).IsAwaitingApproval);

    // Amendment is allowed while Raised or Rejected; everything else is locked —
    // except manual (historic) entries, which stay editable because correcting
    // history is their whole point.
    [Theory]
    [InlineData(ValuationInvoiceStatus.Raised, true)]
    [InlineData(ValuationInvoiceStatus.Rejected, true)]
    [InlineData(ValuationInvoiceStatus.Submitted, false)]
    [InlineData(ValuationInvoiceStatus.Approved, false)]
    [InlineData(ValuationInvoiceStatus.Issued, false)]
    [InlineData(ValuationInvoiceStatus.Paid, false)]
    public void IsEditable_followsStatusForSystemInvoices(ValuationInvoiceStatus status, bool expected) =>
        Assert.Equal(expected, Invoice(status).IsEditable);

    [Theory]
    [InlineData(ValuationInvoiceStatus.Issued)]
    [InlineData(ValuationInvoiceStatus.Paid)]
    public void IsEditable_manualInvoicesStayEditable(ValuationInvoiceStatus status) =>
        Assert.True(Invoice(status, isManual: true).IsEditable);

    // The manual-entry regression the feature exists for: once the historic Issued/Paid
    // invoices are keyed in, "Certified to date" equals everything genuinely invoiced
    // over the years and the next claim's Payment Due shows the true balance
    // outstanding. Certified counts Issued + Paid only — Raised drafts, invoices still
    // in approval, and Cancelled ones stay out.
    [Fact]
    public void CertifiedToDate_fromManualInvoices_yieldsCorrectPaymentDue()
    {
        var invoices = new[]
        {
            Invoice(ValuationInvoiceStatus.Paid, isManual: true, amount: 400_000m, amountPaid: 400_000m),
            Invoice(ValuationInvoiceStatus.Paid, isManual: true, amount: 350_000m, amountPaid: 350_000m),
            Invoice(ValuationInvoiceStatus.Issued, isManual: true, amount: 250_000m),
            Invoice(ValuationInvoiceStatus.Raised, amount: 99_999m),      // not yet certified
            Invoice(ValuationInvoiceStatus.Submitted, amount: 88_888m),   // awaiting approval — not certified
            Invoice(ValuationInvoiceStatus.Cancelled, amount: 77_777m)    // withdrawn — never counts
        };

        var certifiedToDate = invoices
            .Where(invoice => invoice.Status is ValuationInvoiceStatus.Issued or ValuationInvoiceStatus.Paid)
            .Sum(invoice => invoice.Amount);
        Assert.Equal(1_000_000m, certifiedToDate);

        // A £1.25m-works-complete claim at 5% retention against that history:
        var totalWorksComplete = 1_250_000m;
        var retentionHeld = ValuationCalculations.RetentionHeld(totalWorksComplete, 5m);
        var paymentDue = ValuationCalculations.PaymentDueExVat(totalWorksComplete, retentionHeld, 0m, certifiedToDate);

        Assert.Equal(62_500m, retentionHeld);
        Assert.Equal(187_500m, paymentDue); // 1,250,000 − 62,500 − 1,000,000
    }

    // The pending exposure figure: Submitted + Approved, never Cancelled.
    [Fact]
    public void AwaitingApproval_sumsSubmittedAndApprovedOnly()
    {
        var invoices = new[]
        {
            Invoice(ValuationInvoiceStatus.Submitted, amount: 60_000m),
            Invoice(ValuationInvoiceStatus.Approved, amount: 40_000m),
            Invoice(ValuationInvoiceStatus.Raised, amount: 10_000m),
            Invoice(ValuationInvoiceStatus.Issued, amount: 20_000m),
            Invoice(ValuationInvoiceStatus.Cancelled, amount: 30_000m)
        };

        var awaiting = invoices.Where(invoice => invoice.IsAwaitingApproval).Sum(invoice => invoice.Amount);
        Assert.Equal(100_000m, awaiting);
    }
}
