using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai.Tools.Actions;
using Jewel.JPMS.Api.Features.Variations.Commands;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>2026-09-24, Nigel: "we want to be able to unreject a variation order". A reinstated
/// variation returns to Issued, else Quoting, and one rejected after approval comes back
/// unapproved, its approval and the rejection's offset gone from the CVR together.</summary>
public sealed class ReinstateVariationOrderTests
{
    private const string Project = "P-1";
    private const string VariationId = "vo-7";
    private const string VariationCategory = "Variation";

    [Fact]
    public async Task AnIssuedVariation_rejectedBeforeApproval_returnsToIssued()
    {
        await using var context = NewContext();
        context.VariationOrders.Add(Rejected(issuedAt: DateTimeOffset.UnixEpoch));
        await context.SaveChangesAsync();

        var reinstated = await Reinstate(context);

        Assert.Equal(VariationOrderStatus.Issued, reinstated.Status);
        Assert.Null(reinstated.RejectedAt);
        Assert.Equal(DateTimeOffset.UnixEpoch, reinstated.IssuedAt);
    }

    [Fact]
    public async Task AVariationNeverIssued_returnsToQuoting()
    {
        await using var context = NewContext();
        context.VariationOrders.Add(Rejected(issuedAt: null));
        await context.SaveChangesAsync();

        var reinstated = await Reinstate(context);

        Assert.Equal(VariationOrderStatus.Quoting, reinstated.Status);
    }

    [Fact]
    public async Task AVariationRejectedAfterApproval_comesBackUnapproved_andItsAccrualPairLeavesTheCvr()
    {
        await using var context = NewContext();
        var order = Rejected(issuedAt: DateTimeOffset.UnixEpoch);
        order.VariationRef = "V7";
        order.Value = 1_200m;
        order.CostCode = "C100";
        order.ApprovedAt = DateTimeOffset.UnixEpoch;
        order.ApprovedByEmail = "pm@jewelbb.co.uk";
        context.VariationOrders.Add(order);
        context.QsAccruals.AddRange(
            Accrual("a-1", "V7 — Rooflight", add: 1_200m, omit: 0m),
            Accrual("a-2", "V7 — Rooflight (rejected)", add: 0m, omit: 1_200m),
            Accrual("a-3", "V70 — Another variation", add: 500m, omit: 0m));
        await context.SaveChangesAsync();

        var reinstated = await Reinstate(context);

        Assert.Equal(VariationOrderStatus.Issued, reinstated.Status);
        Assert.Null(reinstated.VariationRef);
        Assert.Equal(0m, reinstated.Value);
        Assert.Null(reinstated.ApprovedAt);
        var remaining = await context.QsAccruals.Select(accrual => accrual.QsAccrualId).ToListAsync();
        Assert.Equal(new[] { "a-3" }, remaining);
    }

    [Fact]
    public async Task AVariationThatIsNotRejected_isRefused()
    {
        await using var context = NewContext();
        var order = Rejected(issuedAt: null);
        order.Status = (int)VariationOrderStatus.Issued;
        context.VariationOrders.Add(order);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => Reinstate(context));
    }

    [Fact]
    public void Reinstating_reachesTheConnector_forTheTeamThatManagesVariations()
    {
        var action = AiActionRegistry.All.Single(candidate => candidate.Name == "reinstate_variation_order");

        Assert.True(action.VisibleTo.IncludesAny(new[] { Role.ProjectManager }));
        Assert.False(action.VisibleTo.IncludesAny(new[] { Role.Client }));
    }

    private static Task<VariationOrder> Reinstate(JpmsContext context) =>
        new ReinstateVariationOrderHandler(context)
            .HandleAsync(new ReinstateVariationOrder(VariationId), CancellationToken.None);

    private static VariationOrderEntity Rejected(DateTimeOffset? issuedAt) => new()
    {
        VariationOrderId = VariationId,
        ProjectId = Project,
        RequestId = "req-1",
        Number = 7,
        Reference = "VOQ-0007",
        Title = "Rooflight",
        Status = (int)VariationOrderStatus.Rejected,
        CreatedAt = DateTimeOffset.UnixEpoch,
        CreatedByEmail = "qs@jewelbb.co.uk",
        IssuedAt = issuedAt,
        RejectedAt = DateTimeOffset.UnixEpoch
    };

    private static QsAccrualEntity Accrual(string id, string description, decimal add, decimal omit) => new()
    {
        QsAccrualId = id,
        ProjectId = Project,
        Category = VariationCategory,
        Description = description,
        AddAmount = add,
        OmitAmount = omit,
        SignedOffAt = DateTimeOffset.UnixEpoch
    };

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"reinstate-variation-{Guid.NewGuid():N}").Options);
}
