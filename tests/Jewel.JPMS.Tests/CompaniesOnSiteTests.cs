using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Features.Subcontractors.Queries;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// Who is on site with insurance that has lapsed (the Insurance Update task's register): a company is
// on site while it holds a released order on a project still running, and cover has lapsed when a
// current insurance certificate is past its expiry — nothing else counts as either.
public sealed class CompaniesOnSiteTests
{
    private static readonly DateTimeOffset Yesterday = DateTimeOffset.UtcNow.AddDays(-1);

    [Fact]
    public async Task ACompanyIsOnSite_whileItHoldsAReleasedOrderOnAProjectStillRunning()
    {
        await using var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"companies-on-site-{Guid.NewGuid():N}").Options);
        context.Projects.AddRange(Project("live", "Coombe Lane", ProjectStage.LiveDelivery), Project("done", "By France", ProjectStage.Completed));
        context.WorkOrders.AddRange(
            Order("wo-1", "live", "roofco", WorkOrderStatus.Released), Order("wo-2", "done", "roofco", WorkOrderStatus.Released),
            Order("wo-3", "live", "brickco", WorkOrderStatus.Complete), Order("wo-4", "live", "glassco", WorkOrderStatus.Draft));
        await context.SaveChangesAsync();

        var onSite = await new ListCompaniesOnSiteHandler(context).HandleAsync(new ListCompaniesOnSite(), CancellationToken.None);

        var company = Assert.Single(onSite);
        Assert.Equal("roofco", company.SubcontractorId);
        Assert.Equal(new[] { "Coombe Lane" }, company.ProjectNames);
    }

    [Fact]
    public void Cover_hasLapsedOnlyWhenACurrentInsuranceCertificateIsPastItsExpiry()
    {
        Assert.True(Document("Public liability insurance", Yesterday).HasLapsed());
        Assert.False(Document("CSCS card", Yesterday).HasLapsed());
        Assert.False(Document("Insurance", DateTimeOffset.UtcNow.AddDays(10)).HasLapsed());
        Assert.False(Document("Insurance", null).HasLapsed());
        Assert.False((Document("Insurance", Yesterday) with { SupersededAt = DateTimeOffset.UtcNow }).HasLapsed());
    }

    [Fact]
    public void TheLapsedCoverOnSiteRead_reachesTheConnector_forTheRegistersReaders()
    {
        Assert.Equal(AiToolKind.Read, AiToolCatalogue.Find(LapsedCoverOnSite)?.Kind);
        Assert.Contains(LapsedCoverOnSite, ToolsFor(Role.OfficeComplianceCoordinator));
        Assert.DoesNotContain(LapsedCoverOnSite, ToolsFor(Role.Subcontractor));
    }

    private const string LapsedCoverOnSite = "list_lapsed_cover_on_site";

    private static List<string> ToolsFor(Role role) =>
        AiToolCatalogue.ForConnector(new SignedInUser("test@jewelbb.co.uk", "Test User", new[] { role })).Select(tool => tool.Name).ToList();

    private static ProjectEntity Project(string projectId, string name, ProjectStage stage) =>
        new() { ProjectId = projectId, Reference = $"JBB-2026-{projectId}", Name = name, ClientName = "Client", Stage = (int)stage };

    private static WorkOrderEntity Order(string workOrderId, string projectId, string companyId, WorkOrderStatus status) =>
        new() { WorkOrderId = workOrderId, ProjectId = projectId, SubcontractorId = companyId, Status = (int)status };

    private static ComplianceDocument Document(string kind, DateTimeOffset? expiresAt) =>
        new("doc-1", "roofco", kind, "certificate.pdf", expiresAt, DateTimeOffset.UtcNow.AddYears(-1));
}
