using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai.Tools.Actions;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ArchitectInstructions;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-21, from the security review: perform_action ran an action's Authorisation and
// Validation and nothing else, so an external login connected through MCP reached records its
// role admitted on any project. The gateway now runs the same record scope the endpoint does.
public sealed class AiActionScopesTests
{
    private const string Practice = "arch-a";
    private const string TheirProject = "P-THEIRS";
    private const string AnotherProject = "P-SOMEONE-ELSES";
    private const string TheirRequest = "req-theirs";
    private const string AnotherRequest = "req-someone-elses";
    private const string AnotherVariation = "vo-someone-elses";
    private const string AnotherInstruction = "ai-someone-elses";

    [Fact]
    public async Task AnArchitect_isRefusedAnotherPracticesRecords_throughTheGateway()
    {
        await using var context = await SeededContextAsync();
        var architect = Signed(Role.Architect, architectId: Practice);

        Assert.False(await AiActionScopes.AllowsAsync(context, architect,
            new UpdateRequestDetails(AnotherRequest, "R1", "Title", "Body", RequestStatus.Open, null, null, null, false), CancellationToken.None));
        Assert.False(await AiActionScopes.AllowsAsync(context, architect,
            new RejectVariationOrder(AnotherVariation), CancellationToken.None));
        Assert.False(await AiActionScopes.AllowsAsync(context, architect,
            new DeleteArchitectInstruction(AnotherInstruction), CancellationToken.None));
    }

    [Fact]
    public async Task AnArchitect_passesTheGateway_onTheirOwnRecords()
    {
        await using var context = await SeededContextAsync();
        var architect = Signed(Role.Architect, architectId: Practice);

        Assert.True(await AiActionScopes.AllowsAsync(context, architect,
            new PostRequestMessage(TheirRequest, "A note", MessageVisibility.Internal, AuthorEmail: "a@b.c", AuthorName: "A"), CancellationToken.None));
    }

    [Fact]
    public async Task ACommandWithNoScope_passesTheGateway()
    {
        await using var context = await SeededContextAsync();
        var architect = Signed(Role.Architect, architectId: Practice);

        Assert.True(await AiActionScopes.AllowsAsync(context, architect, new object(), CancellationToken.None));
    }

    private static SignedInUser Signed(Role role, string? architectId = null) =>
        new("someone@example.com", "Someone", new[] { role }, ArchitectId: architectId);

    private static async Task<JpmsContext> SeededContextAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"ai-action-scopes-{Guid.NewGuid():N}").Options);
        context.Projects.AddRange(
            new ProjectEntity { ProjectId = TheirProject, Stage = (int)ProjectStage.LiveDelivery,
                PartyKind = (int)PartyKind.Architect, PartyId = Practice },
            new ProjectEntity { ProjectId = AnotherProject, Stage = (int)ProjectStage.LiveDelivery,
                PartyKind = (int)PartyKind.Architect, PartyId = "arch-b" });
        context.Requests.AddRange(
            new RequestEntity { RequestId = TheirRequest, ProjectId = TheirProject },
            new RequestEntity { RequestId = AnotherRequest, ProjectId = AnotherProject });
        context.VariationOrders.Add(new VariationOrderEntity { VariationOrderId = AnotherVariation, ProjectId = AnotherProject });
        context.ArchitectInstructions.Add(new ArchitectInstructionEntity { ArchitectInstructionId = AnotherInstruction, ProjectId = AnotherProject });
        await context.SaveChangesAsync();
        return context;
    }
}
