using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Contracts.Labour;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

// A worker with history cannot be deleted — their record backs recorded cost — so retiring is
// how their contact details leave the portal (data protection, 2026-09-21): email and phone
// cleared, engagement closed, name and timesheets kept. The connector reaches it by name, and
// an inactive worker is exactly who gets retired.
public sealed class RetireWorkerTests
{
    [Fact]
    public async Task Retiring_clearsContactDetails_closesTheEngagement_keepsTheNameAndTime()
    {
        await using var context = await SeededAsync();
        var retired = await Handler(context).HandleAsync(new RetireWorker("w-1"), CancellationToken.None);

        Assert.Equal("Dave Sparks", retired.Name);
        Assert.Equal("", retired.ContactEmail);
        Assert.Equal("", retired.ContactPhone);
        Assert.False(retired.IsActive);
        Assert.NotNull(retired.EngagedTo);
        Assert.True(retired.HasBeenRetired);
        Assert.Equal(1, await context.Timesheets.CountAsync(row => row.WorkerId == "w-1"));
        Assert.Contains(await context.AuditEvents.ToListAsync(), row => row.EventType == (int)AuditEventType.WorkerRetired);
    }

    [Fact]
    public async Task ByName_findsAnInactiveWorker_andRefusesOneAlreadyRetired()
    {
        await using var context = await SeededAsync();
        var byName = new RetireWorkerByNameHandler(context, Handler(context));

        var retired = await byName.HandleAsync(new RetireWorkerByName("dave sparks"), CancellationToken.None);
        Assert.True(retired.HasBeenRetired);
        await Assert.ThrowsAsync<InvalidOperationException>(() => byName.HandleAsync(new RetireWorkerByName("Dave Sparks"), CancellationToken.None));
    }

    private static RetireWorkerHandler Handler(JpmsContext context) =>
        new(context, new AuditTrail(context, new AuditActor { Email = "pm@jewelbb.co.uk" }, NullLogger<AuditTrail>.Instance));

    private static async Task<JpmsContext> SeededAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"retire-{Guid.NewGuid():N}").Options);
        context.Workers.Add(new WorkerEntity { WorkerId = "w-1", Name = "Dave Sparks", HourlyRate = 28m, IsActive = false, ContactEmail = "dave@example.com", ContactPhone = "07700 900001" });
        context.Timesheets.Add(new TimesheetEntity { TimesheetId = "t-1", ProjectId = "p-1", WorkerId = "w-1", Hours = 8m });
        await context.SaveChangesAsync();
        return context;
    }
}
