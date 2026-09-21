using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Api.Features.Commercial.Commands;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

// A locked valuation's footer is what the client was told. Abbot Road's Valuation 15 was locked at
// net variations £42,125.85 and a variation approved before the client paid re-wrote that to
// £49,712.55 at Confirm (2026-09-18). The contract context freezes at lock; only a fresh lock reads the bill.
public sealed class LockedClaimContractContextTests
{
    private const string Project = "P-AR";
    private const string Claim = "claim-15";
    private const string Slab = "line-slab", FirstVariation = "line-v71", LateVariation = "line-v72";

    [Fact]
    public async Task Confirm_keepsTheContractContextTheClaimWasLockedWith()
    {
        await using var context = await AbbotRoadAsync();
        var claim = await context.ValuationClaims.SingleAsync(row => row.ValuationClaimId == Claim);
        await LockAsync(context);
        Assert.Equal(298_946.04m, claim.ContractSum);
        Assert.Equal(42_125.85m, claim.NetVariations);
        Assert.Equal(341_071.89m, claim.RevisedContractSum);

        await ApproveALateVariationAsync(context);
        await new ConfirmValuationClaimHandler(context).HandleAsync(new ConfirmValuationClaim(Claim), CancellationToken.None);

        Assert.Equal(42_125.85m, claim.NetVariations);
        Assert.Equal(341_071.89m, claim.RevisedContractSum);
        Assert.Equal((int)ValuationClaimStatus.Confirmed, claim.Status);
    }

    [Fact]
    public async Task ConfirmStraightFromDraft_isTheLock_andReadsTheBillAsItStands()
    {
        await using var context = await AbbotRoadAsync();
        var claim = await context.ValuationClaims.SingleAsync(row => row.ValuationClaimId == Claim);
        await ApproveALateVariationAsync(context);

        await new ConfirmValuationClaimHandler(context).HandleAsync(new ConfirmValuationClaim(Claim), CancellationToken.None);

        Assert.Equal(49_712.55m, claim.NetVariations);
        Assert.Equal(348_658.59m, claim.RevisedContractSum);
        Assert.NotNull(claim.LockedAt);
    }

    [Fact]
    public async Task RefreshingALockedClaim_keepsTheContractContext_andRecomputesTheRest()
    {
        await using var context = await AbbotRoadAsync();
        var claim = await context.ValuationClaims.SingleAsync(row => row.ValuationClaimId == Claim);
        await LockAsync(context);
        await ApproveALateVariationAsync(context);

        await ValuationClaimSummary.RefreshTotalsAsync(context, claim, CancellationToken.None);

        Assert.Equal(42_125.85m, claim.NetVariations);
        Assert.Equal(341_071.89m, claim.RevisedContractSum);
        Assert.Equal(150_000m, claim.TotalWorksComplete);
    }

    private static async Task LockAsync(JpmsContext context)
    {
        var audit = new AuditTrail(context, new AuditActor { Email = "qs@jewelbb.co.uk" }, NullLogger<AuditTrail>.Instance);
        await new PreapproveValuationClaimHandler(context, audit).HandleAsync(new PreapproveValuationClaim(Claim), CancellationToken.None);
    }

    private static async Task ApproveALateVariationAsync(JpmsContext context)
    {
        context.ValuationLineItems.Add(new ValuationLineItemEntity
        {
            ValuationLineItemId = LateVariation, ProjectId = Project, ElementType = (int)ValuationElementType.Variation,
            SectionCode = "V", SectionName = "Variations", CostCode = "VAR", Description = "V72 Garden room glazing",
            Unit = "item", Quantity = 1m, Rate = 7_586.70m, LineAmount = 7_586.70m, DisplayOrder = 3
        });
        await context.SaveChangesAsync();
    }

    private static async Task<JpmsContext> AbbotRoadAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"locked-claim-contract-context-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Reference = "JBB-2025-004", Name = "Abbot Road", ClientName = "Client" });
        context.ValuationClaims.Add(new ValuationClaimEntity { ValuationClaimId = Claim, ProjectId = Project, ClaimNumber = 15, Name = "Valuation 15 - Sept 2026", Status = (int)ValuationClaimStatus.Draft, RetentionPercent = 5m });
        context.ValuationLineItems.Add(new ValuationLineItemEntity { ValuationLineItemId = Slab, ProjectId = Project, ElementType = (int)ValuationElementType.ContractWorks, SectionCode = "A", SectionName = "Works", CostCode = "GW", Description = "Contract works", Unit = "item", Quantity = 1m, Rate = 298_946.04m, LineAmount = 298_946.04m, DisplayOrder = 1 });
        context.ValuationLineItems.Add(new ValuationLineItemEntity { ValuationLineItemId = FirstVariation, ProjectId = Project, ElementType = (int)ValuationElementType.Variation, SectionCode = "V", SectionName = "Variations", CostCode = "VAR", Description = "V71 Extra drainage", Unit = "item", Quantity = 1m, Rate = 42_125.85m, LineAmount = 42_125.85m, DisplayOrder = 2 });
        context.ClaimLines.Add(new ClaimLineEntity { ClaimLineId = "cl-15-slab", ValuationClaimId = Claim, ValuationLineItemId = Slab, PercentComplete = 50m, CumulativeClaimed = 149_473.02m, PeriodIncrement = 149_473.02m });
        context.ClaimLines.Add(new ClaimLineEntity { ClaimLineId = "cl-15-v71", ValuationClaimId = Claim, ValuationLineItemId = FirstVariation, PercentComplete = 1.25m, CumulativeClaimed = 526.98m, PeriodIncrement = 526.98m });
        await context.SaveChangesAsync();
        return context;
    }
}
