using Jewel.JPMS.Contracts.SiteAccess;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.SiteAccess;

public sealed class HttpSiteDrawingLinkStore : ISiteDrawingLinkStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpSiteDrawingLinkStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public Task<IReadOnlyList<SiteDrawingLink>> ListAsync(string projectId, CancellationToken cancellationToken) =>
        queries.AskAsync(new ListSiteDrawingLinksForProject(projectId), cancellationToken);

    public Task<SiteDrawingLinkCreated> CreateAsync(CreateSiteDrawingLink command, CancellationToken cancellationToken) =>
        commands.SendAsync(command, cancellationToken);

    public Task RevokeAsync(string siteDrawingLinkId, CancellationToken cancellationToken) =>
        commands.SendAsync(new RevokeSiteDrawingLink(siteDrawingLinkId), cancellationToken);
}
