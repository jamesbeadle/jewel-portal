using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.ArchitectInstructions;
using Jewel.JPMS.Api.Features.Architects.Commands;
using Jewel.JPMS.Api.Features.Closeout;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-21: an architect login belongs to an architect. Before ArchitectId a Role.Architect
// session could edit any project's requests, instructions and variations; now it reaches the
// projects that name its practice as the party, and nothing else. A subcontractor raises a
// request only where they hold an issued work order; an external login with no link reaches
// nothing at all.
public sealed class ArchitectScopeTests
{
    private const string Practice = "arch-a";
    private const string AnotherPractice = "arch-b";
    private const string TheirProject = "P-THEIRS";
    private const string AnotherProject = "P-SOMEONE-ELSES";
    private const string ALeadProject = "P-LEAD";
    private const string TheirRequest = "req-theirs";
    private const string AnotherRequest = "req-someone-elses";
    private const string TheirVariation = "vo-theirs";
    private const string AnotherVariation = "vo-someone-elses";
    private const string TheirInstruction = "ai-theirs";
    private const string AnotherInstruction = "ai-someone-elses";
    private const string Trade = "sub-a";

    [Fact]
    public void AnArchitectLoginWithoutAPractice_isNotAnArchitect()
    {
        Assert.Null(ArchitectScope.OwnArchitectId(Signed(Role.Architect)));
        Assert.Null(ArchitectScope.OwnArchitectId(Signed(Role.ProjectManager, architectId: Practice)));
        Assert.Equal(Practice, ArchitectScope.OwnArchitectId(Signed(Role.Architect, architectId: Practice)));
    }

    [Fact]
    public async Task AnArchitect_raisesARequestOnTheirOwnProject_only()
    {
        await using var context = await SeededContextAsync();
        var architect = Signed(Role.Architect, architectId: Practice);

        Assert.True(await RequestScope.MayRaiseOnProjectAsync(context, architect, TheirProject, CancellationToken.None));
        Assert.False(await RequestScope.MayRaiseOnProjectAsync(context, architect, AnotherProject, CancellationToken.None));
        Assert.False(await RequestScope.MayRaiseOnProjectAsync(context, architect, ALeadProject, CancellationToken.None));
    }

    [Fact]
    public async Task AnArchitect_editsTheirOwnRequests_only()
    {
        await using var context = await SeededContextAsync();
        var architect = Signed(Role.Architect, architectId: Practice);

        Assert.True(await RequestScope.MayActOnAsync(context, architect, TheirRequest, CancellationToken.None));
        Assert.False(await RequestScope.MayActOnAsync(context, architect, AnotherRequest, CancellationToken.None));
    }

    [Fact]
    public async Task AnArchitect_filesInstructionsOnTheirOwnProject_only()
    {
        await using var context = await SeededContextAsync();
        var architect = Signed(Role.Architect, architectId: Practice);

        Assert.True(await ArchitectInstructionScope.MayFileOnProjectAsync(context, architect, TheirProject, CancellationToken.None));
        Assert.False(await ArchitectInstructionScope.MayFileOnProjectAsync(context, architect, AnotherProject, CancellationToken.None));
        Assert.True(await ArchitectInstructionScope.MayActOnAsync(context, architect, TheirInstruction, CancellationToken.None));
        Assert.False(await ArchitectInstructionScope.MayActOnAsync(context, architect, AnotherInstruction, CancellationToken.None));
    }

    [Fact]
    public async Task AnArchitect_actsOnTheirOwnVariations_only()
    {
        await using var context = await SeededContextAsync();
        var architect = Signed(Role.Architect, architectId: Practice);

        Assert.True(await VariationOrderScope.MayActOnAsync(context, architect, TheirVariation, CancellationToken.None));
        Assert.False(await VariationOrderScope.MayActOnAsync(context, architect, AnotherVariation, CancellationToken.None));
    }

    [Fact]
    public async Task AnArchitect_raisesADefectOnTheirOwnProject_only()
    {
        await using var context = await SeededContextAsync();
        var architect = Signed(Role.Architect, architectId: Practice);

        Assert.True(await DefectScope.IsOnTheirOwnProjectAsync(context, architect, TheirProject, CancellationToken.None));
        Assert.False(await DefectScope.IsOnTheirOwnProjectAsync(context, architect, AnotherProject, CancellationToken.None));
    }

    [Fact]
    public async Task AnExternalLoginWithNoLink_reachesNothing()
    {
        await using var context = await SeededContextAsync();

        Assert.False(await RequestScope.MayRaiseOnProjectAsync(context, Signed(Role.Architect), TheirProject, CancellationToken.None));
        Assert.False(await RequestScope.MayActOnAsync(context, Signed(Role.Architect), TheirRequest, CancellationToken.None));
        Assert.False(await DefectScope.IsOnTheirOwnProjectAsync(context, Signed(Role.Client), TheirProject, CancellationToken.None));
    }

    [Fact]
    public async Task ASubcontractor_raisesARequestWhereTheyHoldAnIssuedOrder_only()
    {
        await using var context = await SeededContextAsync();
        var trade = Signed(Role.Subcontractor, subcontractorId: Trade);

        Assert.True(await RequestScope.MayRaiseOnProjectAsync(context, trade, TheirProject, CancellationToken.None));
        Assert.False(await RequestScope.MayRaiseOnProjectAsync(context, trade, AnotherProject, CancellationToken.None));
    }

    [Fact]
    public async Task AnInternalRole_worksEveryProject()
    {
        await using var context = await SeededContextAsync();
        var projectManager = Signed(Role.ProjectManager);

        Assert.True(await RequestScope.MayRaiseOnProjectAsync(context, projectManager, AnotherProject, CancellationToken.None));
        Assert.True(await ArchitectInstructionScope.MayActOnAsync(context, projectManager, AnotherInstruction, CancellationToken.None));
        Assert.True(await VariationOrderScope.MayActOnAsync(context, projectManager, AnotherVariation, CancellationToken.None));
    }

    [Fact]
    public void AnInvite_refusesALoginThatBelongsElsewhere()
    {
        Assert.Null(ArchitectInviteRefusals.For(null, Practice));
        Assert.Null(ArchitectInviteRefusals.For(new DirectoryUserEntity { ArchitectId = Practice }, Practice));
        Assert.NotNull(ArchitectInviteRefusals.For(new DirectoryUserEntity { ArchitectId = AnotherPractice }, Practice));
        Assert.NotNull(ArchitectInviteRefusals.For(new DirectoryUserEntity { ClientId = "client-a" }, Practice));
        Assert.NotNull(ArchitectInviteRefusals.For(new DirectoryUserEntity { SubcontractorId = Trade }, Practice));
        Assert.NotNull(ArchitectInviteRefusals.For(new DirectoryUserEntity { RevokedAt = DateTimeOffset.UtcNow }, Practice));
    }

    private static SignedInUser Signed(Role role, string? subcontractorId = null, string? architectId = null) =>
        new("someone@example.com", "Someone", new[] { role },
            SubcontractorId: subcontractorId, ArchitectId: architectId);

    private static async Task<JpmsContext> SeededContextAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"architect-scope-{Guid.NewGuid():N}").Options);
        context.Projects.AddRange(
            new ProjectEntity { ProjectId = TheirProject, Stage = (int)ProjectStage.LiveDelivery,
                PartyKind = (int)PartyKind.Architect, PartyId = Practice },
            new ProjectEntity { ProjectId = AnotherProject, Stage = (int)ProjectStage.LiveDelivery,
                PartyKind = (int)PartyKind.Architect, PartyId = AnotherPractice },
            new ProjectEntity { ProjectId = ALeadProject, Stage = (int)ProjectStage.Lead,
                PartyKind = (int)PartyKind.Architect, PartyId = Practice });
        context.Requests.AddRange(
            new RequestEntity { RequestId = TheirRequest, ProjectId = TheirProject },
            new RequestEntity { RequestId = AnotherRequest, ProjectId = AnotherProject });
        context.VariationOrders.AddRange(
            new VariationOrderEntity { VariationOrderId = TheirVariation, ProjectId = TheirProject },
            new VariationOrderEntity { VariationOrderId = AnotherVariation, ProjectId = AnotherProject });
        context.ArchitectInstructions.AddRange(
            new ArchitectInstructionEntity { ArchitectInstructionId = TheirInstruction, ProjectId = TheirProject },
            new ArchitectInstructionEntity { ArchitectInstructionId = AnotherInstruction, ProjectId = AnotherProject });
        context.WorkOrders.Add(new WorkOrderEntity
        {
            WorkOrderId = "wo-1", ProjectId = TheirProject, SubcontractorId = Trade, Status = (int)WorkOrderStatus.Released
        });
        await context.SaveChangesAsync();
        return context;
    }
}
