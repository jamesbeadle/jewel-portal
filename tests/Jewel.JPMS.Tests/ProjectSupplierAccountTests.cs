using Jewel.JPMS.Api.Features.Commercial.Queries;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The supplier account's pure rules (2026-09-14, the accountant's ask): where a received invoice
/// stands, whether an unallocated bill belongs on the account, and how the PDF is named. The
/// arithmetic the two halves add up to is ProjectSupplierAccountArithmeticTests.
/// </summary>
public class ProjectSupplierAccountTests
{
    private const string ByFrance = "project-by-france";

    [Theory]
    [InlineData("DRAFT", 0, ProjectSupplierInvoiceState.AwaitingApproval)]
    [InlineData("SUBMITTED", 0, ProjectSupplierInvoiceState.AwaitingApproval)]
    [InlineData("AUTHORISED", 0, ProjectSupplierInvoiceState.AwaitingPayment)]
    [InlineData("AUTHORISED", 0.5, ProjectSupplierInvoiceState.PartPaid)]
    [InlineData("PAID", 1, ProjectSupplierInvoiceState.Paid)]
    public void InvoiceState_readsXeroStatusThenSettlement(string xeroStatus, double settledFraction, ProjectSupplierInvoiceState expected)
    {
        Assert.Equal(expected, ProjectSupplierInvoiceStates.For(xeroStatus, (decimal)settledFraction));
    }

    [Fact]
    public void UnallocatedBill_isPlacedByTheAllocationPagesOwnPrecedence()
    {
        var ourNumbers = new HashSet<int> { 24, 41 };

        // The site (or the project set in the queue) decides first.
        Assert.Equal(ProjectSupplierInvoicePlacement.AwaitingAllocation,
            SupplierBillPlacement.Decide(ByFrance, ByFrance, Array.Empty<int>(), ourNumbers, isSupplierOnlyHere: false));
        Assert.Null(SupplierBillPlacement.Decide(ByFrance, "project-ravenswood", new[] { 24 }, ourNumbers, isSupplierOnlyHere: true));

        // Then a work-order number written on the bill.
        Assert.Equal(ProjectSupplierInvoicePlacement.AwaitingAllocation,
            SupplierBillPlacement.Decide(ByFrance, null, new[] { 41 }, ourNumbers, isSupplierOnlyHere: false));
        Assert.Null(SupplierBillPlacement.Decide(ByFrance, null, new[] { 55 }, ourNumbers, isSupplierOnlyHere: true));

        // Then only the fact that the supplier works nowhere else.
        Assert.Equal(ProjectSupplierInvoicePlacement.AssumedFromSupplier,
            SupplierBillPlacement.Decide(ByFrance, null, Array.Empty<int>(), ourNumbers, isSupplierOnlyHere: true));
        Assert.Null(SupplierBillPlacement.Decide(ByFrance, null, Array.Empty<int>(), ourNumbers, isSupplierOnlyHere: false));
    }

    [Fact]
    public void FileName_isProjectSupplierDocumentDate()
    {
        var name = ProjectSupplierAccountFileNames.For(
            "JBB-2026-001", "By France", "S Williams Plumbing & Heating", new DateTimeOffset(2026, 9, 14, 14, 28, 0, TimeSpan.Zero));
        Assert.Equal("JBB-2026-001 - By France - S Williams Plumbing & Heating - Supplier account 2026-09-14", name);
    }
}
