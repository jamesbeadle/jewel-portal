using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.RecordLinks.Providers;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-15, the Sales pane: an estimate enquiry is tagged to the sales lead it is about, so a
// lead is a linkable record — one that belongs to NO project. The provider lists the company-wide
// register whatever project it is handed, projects every lead with an empty ProjectId (a Won
// lead's mail belongs to its project's records from then on, so Won leads are not offered), and
// resolves the LD-#### tag stem back to the lead.
public sealed class LeadLinkProviderTests
{
    [Fact]
    public async Task ListsTheWholeRegister_withoutAProject_newestFirst_leavingWonOut()
    {
        await using var context = await ContextWithLeadsAsync();
        var provider = new LeadLinkProvider(context);

        var withBlankProject = await provider.ForProjectAsync("", CancellationToken.None);
        var withSomeProject = await provider.ForProjectAsync("P-ANY", CancellationToken.None);

        Assert.Equal(new[] { "LD-0009", "LD-0007" }, withBlankProject.Select(record => record.Reference).ToArray());
        Assert.Equal(withBlankProject.Select(record => record.RecordId), withSomeProject.Select(record => record.RecordId));
        Assert.All(withBlankProject, record => Assert.Equal("", record.ProjectId));
        Assert.All(withBlankProject, record => Assert.Equal(RecordType.Lead, record.Type));
        Assert.DoesNotContain(withBlankProject, record => record.Reference == "LD-0008");
    }

    [Fact]
    public async Task ProjectsTheLead_asItsTagAndTitle()
    {
        await using var context = await ContextWithLeadsAsync();
        var provider = new LeadLinkProvider(context);

        var engaged = await provider.FindAsync("lead-7", CancellationToken.None);
        var nurtured = await provider.FindAsync("lead-9", CancellationToken.None);
        var won = await provider.FindAsync("lead-8", CancellationToken.None);

        Assert.Equal("LD-0007", engaged!.TagReference);
        Assert.Equal("Jane Coombe — 12 Coombe Lane, Kingston", engaged.Title);
        Assert.Equal("Engaged", engaged.StatusLabel);
        Assert.True(engaged.IsActive);
        Assert.False(nurtured!.IsActive);
        // Even a Won lead carries no project: its project's own records own the mail now.
        Assert.Equal("", won!.ProjectId);
        Assert.False(won.IsActive);
    }

    [Fact]
    public async Task FindsTheLead_byItsTagStem()
    {
        await using var context = await ContextWithLeadsAsync();
        var provider = new LeadLinkProvider(context);

        var found = await provider.FindByTagAsync("LD-0007", CancellationToken.None);
        Assert.Equal("lead-7", found!.RecordId);
        Assert.Null(await provider.FindByTagAsync("LD-0042", CancellationToken.None));
        Assert.Null(await provider.FindByTagAsync("WO-0007", CancellationToken.None));
    }

    private static async Task<JpmsContext> ContextWithLeadsAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"lead-link-provider-{Guid.NewGuid():N}").Options);
        context.Leads.Add(Lead("lead-7", 7, "Jane Coombe", "12 Coombe Lane, Kingston", LeadStage.Engaged));
        context.Leads.Add(Lead("lead-8", 8, "Won Client", "1 Built Road", LeadStage.Won, projectId: "P-WON"));
        context.Leads.Add(Lead("lead-9", 9, "Parked Prospect", "", LeadStage.Nurture));
        await context.SaveChangesAsync();
        return context;
    }

    private static LeadEntity Lead(string id, int number, string contact, string address, LeadStage stage, string? projectId = null) =>
        new()
        {
            LeadId = id,
            Number = number,
            ContactName = contact,
            SiteAddress = address,
            Summary = "Rear extension and loft",
            Stage = (int)stage,
            ProjectId = projectId,
            CapturedAt = DateTimeOffset.UtcNow
        };
}
