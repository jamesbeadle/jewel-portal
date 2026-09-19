using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Closeout;
using Jewel.JPMS.Api.Features.Procurement;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-19, from the system-wide permission check: an external role's gate admits it for ANY
// record id. A tenderer could price a competitor's quote or submit one in a competitor's name; a
// client could raise a defect on somebody else's house. The role says what kind of work they may
// do; these scopes say whose.
public sealed class ExternalWriteScopeTests
{
    private const string TheirPackage = "bp-theirs";
    private const string AnotherPackage = "bp-someone-elses";
    private const string Tenderer = "sub-a";
    private const string Competitor = "sub-b";
    private const string TheirQuote = "q-theirs";
    private const string CompetitorsQuote = "q-competitors";
    private const string TheirProject = "P-THEIRS";
    private const string AnotherProject = "P-SOMEONE-ELSES";
    private const string TheirClient = "client-a";

    [Fact]
    public async Task ATenderer_pricesTheirOwnInvitation_only()
    {
        await using var context = await SeededContextAsync();
        var tenderer = Signed(Role.Subcontractor, subcontractorId: Tenderer);

        Assert.True(await QuoteScope.MayPriceBidPackageAsync(
            context, tenderer, TheirPackage, Tenderer, CancellationToken.None));
        Assert.False(await QuoteScope.MayPriceBidPackageAsync(
            context, tenderer, AnotherPackage, Tenderer, CancellationToken.None));
    }

    [Fact]
    public async Task ATenderer_cannotQuoteInACompetitorsName()
    {
        await using var context = await SeededContextAsync();
        var tenderer = Signed(Role.Subcontractor, subcontractorId: Tenderer);

        Assert.False(await QuoteScope.MayPriceBidPackageAsync(
            context, tenderer, TheirPackage, Competitor, CancellationToken.None));
    }

    [Fact]
    public async Task ATenderer_revisesTheirOwnQuote_only()
    {
        await using var context = await SeededContextAsync();
        var tenderer = Signed(Role.Subcontractor, subcontractorId: Tenderer);

        Assert.True(await QuoteScope.MayReviseQuoteAsync(
            context, tenderer, TheirQuote, CancellationToken.None));
        Assert.False(await QuoteScope.MayReviseQuoteAsync(
            context, tenderer, CompetitorsQuote, CancellationToken.None));
    }

    [Fact]
    public async Task AnInternalRole_pricesAndRevisesEveryPackage()
    {
        await using var context = await SeededContextAsync();
        var projectManager = Signed(Role.ProjectManager);

        Assert.True(await QuoteScope.MayPriceBidPackageAsync(
            context, projectManager, AnotherPackage, Competitor, CancellationToken.None));
        Assert.True(await QuoteScope.MayReviseQuoteAsync(
            context, projectManager, CompetitorsQuote, CancellationToken.None));
    }

    [Fact]
    public async Task AClient_raisesADefectOnTheirOwnProject_only()
    {
        await using var context = await SeededContextAsync();
        var client = Signed(Role.Client, clientId: TheirClient);

        Assert.True(await DefectScope.IsOnTheirOwnProjectAsync(
            context, client, TheirProject, CancellationToken.None));
        Assert.False(await DefectScope.IsOnTheirOwnProjectAsync(
            context, client, AnotherProject, CancellationToken.None));
    }

    private static SignedInUser Signed(Role role, string? subcontractorId = null, string? clientId = null) =>
        new("someone@example.com", "Someone", new[] { role },
            SubcontractorId: subcontractorId, ClientId: clientId);

    private static async Task<JpmsContext> SeededContextAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"external-write-scope-{Guid.NewGuid():N}").Options);
        context.BidPackageRecipients.Add(new BidPackageRecipientEntity
        {
            RecipientId = "r-1", BidPackageId = TheirPackage, SubcontractorId = Tenderer
        });
        context.Quotes.AddRange(
            new QuoteEntity { QuoteId = TheirQuote, BidPackageId = TheirPackage, SubcontractorId = Tenderer },
            new QuoteEntity { QuoteId = CompetitorsQuote, BidPackageId = TheirPackage, SubcontractorId = Competitor });
        context.Projects.AddRange(
            new ProjectEntity { ProjectId = TheirProject, Stage = (int)ProjectStage.LiveDelivery,
                PartyKind = (int)PartyKind.Client, PartyId = TheirClient },
            new ProjectEntity { ProjectId = AnotherProject, Stage = (int)ProjectStage.LiveDelivery,
                PartyKind = (int)PartyKind.Client, PartyId = "client-b" });
        await context.SaveChangesAsync();
        return context;
    }
}
