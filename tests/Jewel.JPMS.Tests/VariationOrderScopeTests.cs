using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-19, from the system-wide permission check: AllowedToApproveVariations admits Role.Client
// for ANY variation order id, and approval writes the contract figures — so the role gate alone let
// one client approve another client's variation. The role answers "may this kind of user approve a
// variation"; VariationOrderScope answers "is this one theirs".
public sealed class VariationOrderScopeTests
{
    private const string TheirProject = "P-THEIRS";
    private const string AnotherProject = "P-SOMEONE-ELSES";
    private const string TheirVariation = "vo-theirs";
    private const string AnotherVariation = "vo-someone-elses";
    private const string TheirClient = "client-a";

    [Fact]
    public async Task AClient_actsOnTheirOwnVariation_andOnNobodyElses()
    {
        await using var context = await SeededContextAsync();
        var client = Signed(Role.Client, clientId: TheirClient);

        Assert.True(await VariationOrderScope.IsTheirsToActOnAsync(
            context, client, TheirVariation, CancellationToken.None));
        Assert.False(await VariationOrderScope.IsTheirsToActOnAsync(
            context, client, AnotherVariation, CancellationToken.None));
    }

    [Fact]
    public async Task AClientLoginWithNoClientAccount_actsOnNothing()
    {
        await using var context = await SeededContextAsync();
        var unlinked = Signed(Role.Client, clientId: null);

        Assert.False(await VariationOrderScope.IsTheirsToActOnAsync(
            context, unlinked, TheirVariation, CancellationToken.None));
    }

    [Fact]
    public async Task AnInternalRole_worksEveryProject()
    {
        await using var context = await SeededContextAsync();

        foreach (var role in new[] { Role.ProjectManager, Role.QuantitySurveyor, Role.ManagingDirector })
        {
            Assert.True(await VariationOrderScope.IsTheirsToActOnAsync(
                context, Signed(role), AnotherVariation, CancellationToken.None));
        }
    }

    [Fact]
    public async Task ARoleThatMayNotApproveAtAll_actsOnNothing()
    {
        await using var context = await SeededContextAsync();

        Assert.False(await VariationOrderScope.IsTheirsToActOnAsync(
            context, Signed(Role.SiteManager), TheirVariation, CancellationToken.None));
    }

    private static SignedInUser Signed(Role role, string? clientId = null) =>
        new("someone@example.com", "Someone", new[] { role }, ClientId: clientId);

    private static async Task<JpmsContext> SeededContextAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"variation-scope-{Guid.NewGuid():N}").Options);
        context.Projects.AddRange(
            new ProjectEntity
            {
                ProjectId = TheirProject, Stage = (int)ProjectStage.LiveDelivery,
                PartyKind = (int)PartyKind.Client, PartyId = TheirClient
            },
            new ProjectEntity
            {
                ProjectId = AnotherProject, Stage = (int)ProjectStage.LiveDelivery,
                PartyKind = (int)PartyKind.Client, PartyId = "client-b"
            });
        context.VariationOrders.AddRange(
            new VariationOrderEntity
            {
                VariationOrderId = TheirVariation, ProjectId = TheirProject,
                Status = (int)VariationOrderStatus.Issued
            },
            new VariationOrderEntity
            {
                VariationOrderId = AnotherVariation, ProjectId = AnotherProject,
                Status = (int)VariationOrderStatus.Issued
            });
        await context.SaveChangesAsync();
        return context;
    }
}
