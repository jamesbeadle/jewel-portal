using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Commercial.Commands;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// Restating a locked claim's lines (Jeremy, By France, 2026-09-29): Valuation 20 was paid with
/// every V16 line smeared to one uniform percentage, and a confirmed claim cannot reopen. Money
/// may move BETWEEN a locked claim's frozen rows — never in or out, and never across the
/// contract / variation split — and "this period" follows on that claim and the one after.
/// </summary>
public sealed class ClaimRestatementTests
{
    private const string Project = "P-BF";
    private const string V20 = "claim-20", V21 = "claim-21";
    private const string Omit = "v16-omit", Slab = "v16-slab", Wall = "v16-wall";

    [Fact]
    public void Plan_movesMoneyBetweenLines_andSaysWhatWouldLeaveTheClaim()
    {
        var rows = new ClaimRestatement.Row[]
        {
            new("a", 1_000m, CountsTowardTotals: true, IsVariation: true, CumulativeClaimed: 400m),
            new("b", 500m, CountsTowardTotals: true, IsVariation: true, CumulativeClaimed: 300m),
        };

        var balanced = ClaimRestatement.Plan(rows, new Dictionary<string, decimal> { ["a"] = 50m, ["b"] = 40m });
        Assert.Equal(500m, balanced.Entries.Single(entry => entry.ValuationLineItemId == "a").CumulativeClaimed);
        Assert.Equal(200m, balanced.Entries.Single(entry => entry.ValuationLineItemId == "b").CumulativeClaimed);
        Assert.True(balanced.KeepsTheTotal);

        var leaking = ClaimRestatement.Plan(rows, new Dictionary<string, decimal> { ["a"] = 100m, ["b"] = 100m });
        Assert.Equal(800m, leaking.TotalMoved);
        Assert.False(leaking.KeepsTheTotal);
    }

    [Fact]
    public void Plan_ignoresALineThatDoesNotCount_andKeepsContractAndVariationSidesApart()
    {
        var rows = new ClaimRestatement.Row[]
        {
            new("roof", 4_000m, CountsTowardTotals: true, IsVariation: false, CumulativeClaimed: 2_000m),
            new("slab", 1_000m, CountsTowardTotals: true, IsVariation: true, CumulativeClaimed: 0m),
            new("tbc", 999m, CountsTowardTotals: false, IsVariation: true, CumulativeClaimed: 0m),
        };

        var acrossTheSplit = ClaimRestatement.Plan(rows, new Dictionary<string, decimal> { ["roof"] = 0m, ["slab"] = 200m, ["tbc"] = 100m });

        Assert.Equal(0m, acrossTheSplit.TotalMoved);
        Assert.Equal(-2_000m, acrossTheSplit.ContractSideMoved);
        Assert.False(acrossTheSplit.KeepsTheTotal);
    }

    [Fact]
    public async Task Restating_rewritesTheFrozenRows_andThisPeriodOnBothClaims()
    {
        await using var context = await ByFranceAsync();

        var restated = await new RestateValuationClaimLinesHandler(context).HandleAsync(new RestateValuationClaimLines(V20, new[]
        {
            new ClaimEntryInput(Omit, 100m), new ClaimEntryInput(Slab, 100m), new ClaimEntryInput(Wall, 0m),
        }), CancellationToken.None);

        var rows = await context.ClaimLines.Where(row => row.ValuationClaimId == V20).ToListAsync();
        Assert.Equal(-13_175m, rows.Sum(row => row.CumulativeClaimed));
        Assert.Equal(-20_000m, rows.Single(row => row.ValuationLineItemId == Omit).CumulativeClaimed);
        Assert.Equal(6_825m, rows.Single(row => row.ValuationLineItemId == Slab).CumulativeClaimed);
        Assert.Equal("Row 410", rows.Single(row => row.ValuationLineItemId == Slab).Description);
        Assert.Equal(-20_000m, restated.Single(row => row.ValuationLineItemId == Omit).PeriodIncrement);

        var septemberSlab = await context.ClaimLines.SingleAsync(row => row.ValuationClaimId == V21 && row.ValuationLineItemId == Slab);
        Assert.Equal(0m, septemberSlab.PeriodIncrement);
        var august = await context.ValuationClaims.SingleAsync(claim => claim.ValuationClaimId == V20);
        Assert.Equal((int)ValuationClaimStatus.Confirmed, august.Status);
        Assert.Equal(-13_175m, august.TotalWorksComplete);
    }

    [Fact]
    public async Task Restating_isRefused_whenMoneyWouldLeave_onADraft_orForALineTheClaimDoesNotCarry()
    {
        await using var context = await ByFranceAsync();
        var handler = new RestateValuationClaimLinesHandler(context);

        var leaks = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new RestateValuationClaimLines(V20, new[] { new ClaimEntryInput(Slab, 100m) }), CancellationToken.None));
        Assert.Contains("never in total", leaks.Message);
        var slab = await context.ClaimLines.SingleAsync(row => row.ValuationClaimId == V20 && row.ValuationLineItemId == Slab);
        Assert.Equal(1_000m, slab.CumulativeClaimed);

        var draft = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new RestateValuationClaimLines(V21, new[] { new ClaimEntryInput(Slab, 100m) }), CancellationToken.None));
        Assert.Contains("Draft", draft.Message);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.HandleAsync(
            new RestateValuationClaimLines(V20, new[] { new ClaimEntryInput("not-on-it", 0m) }), CancellationToken.None));
    }

    /// <summary>Valuation 20 (Confirmed) carries V16 smeared across the omit, the slab and the wall,
    /// £-13,175 in all; Valuation 21 (Draft) has the slab claimed in full.</summary>
    private static async Task<JpmsContext> ByFranceAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"claim-restatement-{Guid.NewGuid():N}").Options);
        context.ValuationClaims.Add(new ValuationClaimEntity { ValuationClaimId = V20, ProjectId = Project, ClaimNumber = 3, Name = "Valuation 20 - August 2026", Status = (int)ValuationClaimStatus.Confirmed, TotalWorksComplete = -13_175m, LockedAt = DateTimeOffset.UtcNow });
        context.ValuationClaims.Add(new ValuationClaimEntity { ValuationClaimId = V21, ProjectId = Project, ClaimNumber = 4, Name = "Valuation 21 - September 2026", Status = (int)ValuationClaimStatus.Draft });
        context.ClaimLines.Add(FrozenRow("v20-omit", Omit, -20_000m, ValuationLineType.Omit, "Row 405", cumulative: -10_000m));
        context.ClaimLines.Add(FrozenRow("v20-slab", Slab, 6_825m, ValuationLineType.Priced, "Row 410", cumulative: 1_000m));
        context.ClaimLines.Add(FrozenRow("v20-wall", Wall, 13_750m, ValuationLineType.Priced, "Row 440", cumulative: -4_175m));
        context.ClaimLines.Add(new ClaimLineEntity { ClaimLineId = "v21-slab", ValuationClaimId = V21, ValuationLineItemId = Slab, PercentComplete = 100m, CumulativeClaimed = 6_825m, PeriodIncrement = 5_825m });
        await context.SaveChangesAsync();
        return context;
    }

    private static ClaimLineEntity FrozenRow(string id, string lineId, decimal amount, ValuationLineType lineType, string description, decimal cumulative) =>
        new()
        {
            ClaimLineId = id, ValuationClaimId = V20, ValuationLineItemId = lineId,
            ElementType = (int)ValuationElementType.Variation, LineType = (int)lineType, VariationRef = "V16",
            Description = description, LineAmount = amount, Rate = amount, Quantity = 1m,
            PercentComplete = ClaimMoneySpread.Percent(cumulative, amount), CumulativeClaimed = cumulative, PeriodIncrement = cumulative, DisplayOrder = 0
        };
}
