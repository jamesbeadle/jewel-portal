using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Procurement.Acceptance;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

public sealed partial class WorkOrderAcceptanceTests
{
    private const string Token = "the-secret-token";

    [Fact]
    public async Task ThePublicService_acceptsUnderTheTypedName_withTheContactsEmail_andLeavesAnAuditRow()
    {
        await using var context = await ContextWithAnIssuedOrderAsync();
        var service = new WorkOrderAcceptanceService(context, Audit(context));

        var before = await service.ViewAsync(Token, CancellationToken.None);
        Assert.NotNull(before);
        Assert.Equal("Ann Farrant", before!.SupplierContactName);
        Assert.Equal("ann@farrant.test", before.SupplierContactEmail);
        Assert.True(before.Order.IsAwaitingAcceptance);

        var after = await service.AcceptAsync(Token, new WorkOrderAcceptanceSignature("  Bob Farrant "), CancellationToken.None);
        Assert.True(after!.Order.IsAccepted);
        Assert.Equal("Bob Farrant", after.Order.AcceptedByName);
        Assert.Equal("ann@farrant.test", after.Order.AcceptedByEmail);

        var audit = Assert.Single(context.AuditEvents);
        Assert.Equal((int)AuditEventType.WorkOrderAccepted, audit.EventType);
        Assert.Equal("ann@farrant.test", audit.ActorEmail);
        Assert.Equal("wo-1", audit.RecordId);
        Assert.Contains("Bob Farrant", audit.Detail);

        // A second press is the same acceptance, not a second audit row.
        await service.AcceptAsync(Token, new WorkOrderAcceptanceSignature("Carol"), CancellationToken.None);
        Assert.Single(context.AuditEvents);
        Assert.Equal("Bob Farrant", (await context.WorkOrders.SingleAsync()).AcceptedByName);
    }

    [Fact]
    public async Task AnUnknownToken_answersNothing_andABlankNameIsRefused()
    {
        await using var context = await ContextWithAnIssuedOrderAsync();
        var service = new WorkOrderAcceptanceService(context, Audit(context));
        Assert.Null(await service.ViewAsync("not-a-token", CancellationToken.None));
        Assert.Null(await service.ViewAsync("", CancellationToken.None));
        Assert.Null(await service.AcceptAsync("not-a-token", new WorkOrderAcceptanceSignature("Ann"), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AcceptAsync(Token, new WorkOrderAcceptanceSignature("   "), CancellationToken.None));
    }

    [Fact]
    public async Task AClosedOrder_stillShows_butRefusesAcceptance()
    {
        await using var context = await ContextWithAnIssuedOrderAsync();
        var order = await context.WorkOrders.SingleAsync();
        order.Status = (int)WorkOrderStatus.Complete;
        await context.SaveChangesAsync();
        var service = new WorkOrderAcceptanceService(context, Audit(context));

        var view = await service.ViewAsync(Token, CancellationToken.None);
        Assert.False(view!.Order.IsAwaitingAcceptance);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AcceptAsync(Token, new WorkOrderAcceptanceSignature("Ann"), CancellationToken.None));
        Assert.Empty(context.AuditEvents);
    }

    private static AuditTrail Audit(JpmsContext context) =>
        new(context, new AuditActor(), NullLogger<AuditTrail>.Instance);

    private static async Task<JpmsContext> ContextWithAnIssuedOrderAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"work-order-acceptance-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = "P-1", Name = "By France", AddressLine = "1 High Street", Town = "Weybridge", Postcode = "KT13 8AA" });
        context.Subcontractors.Add(new SubcontractorEntity
        {
            SubcontractorId = "sub-farrant", CompanyName = "Farrant Flooring", ContactName = "Ann Farrant",
            ContactEmail = "ann@farrant.test", Category = (int)DirectoryCategory.Subcontractor
        });
        context.WorkOrders.Add(new WorkOrderEntity
        {
            WorkOrderId = "wo-1", ProjectId = "P-1", SubcontractorId = "sub-farrant", Number = 45, Title = "Flooring",
            Status = (int)WorkOrderStatus.Released, Value = 1748m, AwardedAt = Now, CreatedAt = Now,
            AwardedByEmail = "pm@jewelbb.co.uk", AcceptanceToken = Token, AcceptanceTokenIssuedAt = Now
        });
        await context.SaveChangesAsync();
        return context;
    }
}
