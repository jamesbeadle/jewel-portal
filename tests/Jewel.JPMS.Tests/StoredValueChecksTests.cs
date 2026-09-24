using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Data.StoredValues;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-24, V33 on Abbot Road (JPMS-4F2116): a 550-character line description met an
// nvarchar(512) column and the approval failed as "Backend call failure". Line descriptions now
// have no limit, and anything that still does not fit its column is refused before SQL Server
// with a sentence that names the field — never a server error, and never saved cut short.
public sealed class StoredValueChecksTests
{
    private const int V33LineA10Length = 550;
    private const int ProjectNameLength = 256;
    private const decimal TooLargeForEighteenFour = 100_000_000_000_000m;

    [Fact]
    public async Task ALongLineDescription_isStoredWhole()
    {
        await using var context = NewContext();
        var description = new string('a', V33LineA10Length);
        context.ValuationLineItems.Add(Line(description));

        await context.SaveChangesAsync();

        Assert.Equal(description, (await context.ValuationLineItems.SingleAsync()).Description);
    }

    [Fact]
    public async Task ANameLongerThanItsColumn_isRefusedNamingTheField_andNothingIsSaved()
    {
        await using var context = NewContext();
        context.Projects.Add(new ProjectEntity { ProjectId = "p-1", Name = new string('n', ProjectNameLength + 1) });

        var refusal = await Assert.ThrowsAsync<StoredValuesRejectedException>(() => context.SaveChangesAsync());

        var problem = Assert.Single(refusal.Problems);
        Assert.Equal("Name", problem.Field);
        Assert.Equal("Project", problem.Record);
        Assert.Contains("257 characters long; it can hold at most 256", problem.Sentence);
        context.ChangeTracker.Clear();
        Assert.False(await context.Projects.AnyAsync());
    }

    [Fact]
    public async Task ANumberBeyondItsPrecision_isRefused()
    {
        await using var context = NewContext();
        var line = Line("Screed");
        line.Rate = TooLargeForEighteenFour;
        context.ValuationLineItems.Add(line);

        var refusal = await Assert.ThrowsAsync<StoredValuesRejectedException>(() => context.SaveChangesAsync());

        Assert.Equal("Rate", Assert.Single(refusal.Problems).Field);
    }

    [Fact]
    public async Task AnEditThatStillFits_isSaved()
    {
        await using var context = NewContext();
        context.Projects.Add(new ProjectEntity { ProjectId = "p-1", Name = "Abbot Road" });
        await context.SaveChangesAsync();

        var project = await context.Projects.SingleAsync();
        project.Name = new string('n', ProjectNameLength);
        await context.SaveChangesAsync();

        Assert.Equal(ProjectNameLength, (await context.Projects.SingleAsync()).Name.Length);
    }

    private static ValuationLineItemEntity Line(string description) => new()
    {
        ValuationLineItemId = Guid.NewGuid().ToString("N"),
        ProjectId = "p-1",
        Description = description,
        Quantity = 1m,
        Rate = 1m,
        LineAmount = 1m
    };

    private static JpmsContext NewContext() => new(new DbContextOptionsBuilder<JpmsContext>()
        .UseInMemoryDatabase($"stored-values-{Guid.NewGuid():N}").Options);
}
