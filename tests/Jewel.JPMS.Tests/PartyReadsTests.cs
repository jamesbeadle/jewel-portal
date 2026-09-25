using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Parties;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-24: one app, tailored to the role. The project's client and architect open the same RFI
// and variation views as the delivery team; the API confines them to their own projects — for an
// architect, those given to their login in Admin → Users (2026-09-25) — keeps what is still being
// priced from them, and strips what is internal.
public sealed class PartyReadsTests
{
    private const string ArchitectLogin = "architect@practice.example";
    private const string Client = "client-a";
    private const string PartyProject = "P-PARTY";
    private const string ContactProject = "P-CONTACT";
    private const string AnotherProject = "P-SOMEONE-ELSES";

    [Fact]
    public async Task AnArchitect_readsTheVariationsOnTheProjectsTheirLoginWasGiven_onceIssued()
    {
        await using var context = await SeededContextAsync();
        var architect = Signed(Role.Architect, email: ArchitectLogin);

        Assert.True(await PartyReads.MayReadVariationAsync(context, architect, "vo-party", CancellationToken.None));
        Assert.True(await PartyReads.MayReadVariationAsync(context, architect, "vo-contact", CancellationToken.None));
        Assert.False(await PartyReads.MayReadVariationAsync(context, architect, "vo-quoting", CancellationToken.None));
        Assert.False(await PartyReads.MayReadVariationAsync(context, architect, "vo-elsewhere", CancellationToken.None));
    }

    [Fact]
    public async Task AClient_readsTheirOwnProjects_only()
    {
        await using var context = await SeededContextAsync();
        var client = Signed(Role.Client, clientId: Client);

        Assert.True(await PartyReads.MayReadProjectAsync(context, client, ContactProject, CancellationToken.None));
        Assert.False(await PartyReads.MayReadProjectAsync(context, client, PartyProject, CancellationToken.None));
        Assert.True(await PartyReads.MayReadVariationAsync(context, client, "vo-contact", CancellationToken.None));
        Assert.False(await PartyReads.MayReadVariationAsync(context, client, "vo-party", CancellationToken.None));
    }

    [Fact]
    public async Task AnUnlinkedPartyLogin_readsNothing_andTheTeamReadsEverything()
    {
        await using var context = await SeededContextAsync();

        Assert.False(await PartyReads.MayReadProjectAsync(context, Signed(Role.Architect), PartyProject, CancellationToken.None));
        Assert.True(await PartyReads.MayReadVariationAsync(context, Signed(Role.ProjectManager), "vo-quoting", CancellationToken.None));
    }

    [Fact]
    public void WhatLeavesTheServer_forAParty_isStrippedOfWhatIsInternal()
    {
        var order = new VariationOrder("vo", "P", "r", 1, "VOQ-0001", "Title", "", VariationOrderStatus.Issued,
            "bp", "sub", 100m, null, 0m, "CC-1", DateTimeOffset.UtcNow, "pm@jewel");
        var architect = Signed(Role.Architect, email: ArchitectLogin);

        var read = order.AsReadBy(architect);

        Assert.Null(read.CostCode);
        Assert.Null(read.SelectedSubcontractorId);
        Assert.Null(read.SelectedBidPackageId);
        Assert.Equal(100m, read.EstimatedValue);
        Assert.Equal("CC-1", order.AsReadBy(Signed(Role.ProjectManager)).CostCode);
    }

    [Fact]
    public void AnAdministrator_givesTheArchitectByHand_butNoLinkedRole()
    {
        Assert.Contains(Role.Architect, LoginRoles.AssignedByHand);
        Assert.DoesNotContain(Role.Client, LoginRoles.AssignedByHand);
        Assert.DoesNotContain(Role.Subcontractor, LoginRoles.AssignedByHand);
        Assert.True(LoginRoles.IncludeStaff(new[] { Role.Admin }));
        Assert.False(LoginRoles.IncludeStaff(new[] { Role.Architect }));
    }

    private static SignedInUser Signed(Role role, string? clientId = null, string email = "someone@example.com") =>
        new(email, "Someone", new[] { role }, ClientId: clientId);

    private static async Task<JpmsContext> SeededContextAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"party-reads-{Guid.NewGuid():N}").Options);
        context.Projects.AddRange(
            new ProjectEntity { ProjectId = PartyProject, Stage = (int)ProjectStage.LiveDelivery },
            new ProjectEntity { ProjectId = ContactProject, Stage = (int)ProjectStage.LiveDelivery,
                PartyKind = (int)PartyKind.Client, PartyId = Client },
            new ProjectEntity { ProjectId = AnotherProject, Stage = (int)ProjectStage.LiveDelivery });
        context.ProjectAccessGrants.AddRange(
            new ProjectAccessGrantEntity { ProjectAccessGrantId = "g-1", Email = ArchitectLogin, ProjectId = PartyProject },
            new ProjectAccessGrantEntity { ProjectAccessGrantId = "g-2", Email = ArchitectLogin, ProjectId = ContactProject });
        context.VariationOrders.AddRange(
            Order("vo-party", PartyProject, VariationOrderStatus.Issued),
            Order("vo-contact", ContactProject, VariationOrderStatus.Approved),
            Order("vo-quoting", PartyProject, VariationOrderStatus.Quoting),
            Order("vo-elsewhere", AnotherProject, VariationOrderStatus.Issued));
        await context.SaveChangesAsync();
        return context;
    }

    private static VariationOrderEntity Order(string id, string projectId, VariationOrderStatus status) => new()
    {
        VariationOrderId = id, ProjectId = projectId, Status = (int)status, Title = id, CreatedAt = DateTimeOffset.UtcNow
    };
}
