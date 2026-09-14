using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The Invoice document window's bill card read (2026-09-14, the bookkeeper's ask): one bill's
/// stored lines whatever tab each sits on, largest first, an allocated line still carrying its
/// split, nothing from any other bill — and every line shaped exactly as the status read shapes
/// it, so a Work Order bill read whole still says it is one.
/// </summary>
public sealed class ListXeroLedgerLinesForInvoiceTests
{
    [Fact]
    public async Task ReturnsEveryStoredLineOfTheBillWhateverItsStatusLargestFirst()
    {
        using var context = NewContext();
        AddLine(context, "inv-1:a", "inv-1", "Jack Uploaded - Toolstation", 146.17m, XeroAllocationStatus.Unallocated);
        AddLine(context, "inv-1:b", "inv-1", "Screws", 29.23m, XeroAllocationStatus.Allocated);
        AddLine(context, "inv-1:c", "inv-1", "Delivery", 6.00m, XeroAllocationStatus.Ignored);
        AddLine(context, "inv-2:a", "inv-2", "Electrical works", 6000m, XeroAllocationStatus.Unallocated);
        context.XeroCostSplits.Add(new XeroCostSplitEntity
        {
            XeroCostSplitId = "inv-1:b:P1:00005", XeroLedgerLineId = "inv-1:b", ProjectId = "P1", CostCenterCode = "00005", Net = 29.23m
        });
        await context.SaveChangesAsync();

        var lines = await new ListXeroLedgerLinesForInvoiceHandler(context)
            .HandleAsync(new ListXeroLedgerLinesForInvoice("inv-1"), CancellationToken.None);

        Assert.Equal(new[] { "inv-1:a", "inv-1:b", "inv-1:c" }, lines.Select(line => line.XeroLedgerLineId));
        Assert.Equal(
            new[] { XeroAllocationStatus.Unallocated, XeroAllocationStatus.Allocated, XeroAllocationStatus.Ignored },
            lines.Select(line => line.AllocationStatus));
        Assert.Equal("00005", Assert.Single(lines[1].Splits!).CostCenterCode);
    }

    [Fact]
    public async Task ABillReadWholeIsShapedExactlyAsTheQueueReadsIt()
    {
        // The accountant's Work Order bill 1724 lands on the Work Order bills tab from the status
        // read; read whole it must say the same — the connector reads bills this way too.
        var fixture = await WorkOrderBillFixture.CreateAsync();
        var fromQueue = (await fixture.ReadUnallocatedAsync()).Where(line => line.XeroInvoiceId == "inv-1724").ToList();

        var whole = await new ListXeroLedgerLinesForInvoiceHandler(fixture.Context)
            .HandleAsync(new ListXeroLedgerLinesForInvoice("inv-1724"), CancellationToken.None);

        Assert.Equal(fromQueue.Count, whole.Count);
        foreach (var line in whole)
        {
            var queued = Assert.Single(fromQueue, candidate => candidate.XeroLedgerLineId == line.XeroLedgerLineId);
            Assert.Equal(queued.WorkOrderMatch?.WorkOrderId, line.WorkOrderMatch?.WorkOrderId);
            Assert.Equal(XeroLedgerQueues.Of(queued, true), XeroLedgerQueues.Of(line, true));
        }
    }

    [Fact]
    public async Task AnUnknownOrBlankInvoiceIdAnswersWithNoLines()
    {
        using var context = NewContext();
        AddLine(context, "inv-1:a", "inv-1", "Jack Uploaded - Toolstation", 146.17m, XeroAllocationStatus.Unallocated);
        await context.SaveChangesAsync();
        var handler = new ListXeroLedgerLinesForInvoiceHandler(context);

        Assert.Empty(await handler.HandleAsync(new ListXeroLedgerLinesForInvoice("inv-9"), CancellationToken.None));
        Assert.Empty(await handler.HandleAsync(new ListXeroLedgerLinesForInvoice(" "), CancellationToken.None));
    }

    private static JpmsContext NewContext() => new(new DbContextOptionsBuilder<JpmsContext>()
        .UseInMemoryDatabase($"bill-lines-{Guid.NewGuid():N}").Options);

    private static void AddLine(
        JpmsContext context, string lineId, string invoiceId, string description, decimal net, XeroAllocationStatus status)
    {
        context.XeroLedgerLines.Add(new XeroLedgerLineEntity
        {
            XeroLedgerLineId = lineId, XeroInvoiceId = invoiceId, XeroLineItemId = lineId, Type = "ACCPAY",
            InvoiceNumber = "0058/06102965", Reference = "WO-0026", ContactName = "Toolstation", InvoiceStatus = "DRAFT",
            Description = description, Net = net, AllocationStatus = (int)status,
            FirstSeenAtUtc = DateTimeOffset.UtcNow, LastSyncedAtUtc = DateTimeOffset.UtcNow
        });
    }
}
