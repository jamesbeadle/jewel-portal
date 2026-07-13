using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

public sealed class GetSiteAccessEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetSiteAccess, SiteAccess> handler;
    public GetSiteAccessEndpoint(SignedInUserResolver users, IQueryHandler<GetSiteAccess, SiteAccess> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(GetSiteAccess))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/labour/site-access")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new GetSiteAccess(projectId), request.HttpContext.RequestAborted));
    }
}

/// <summary>Returns the project's active token, minting the first one on demand.</summary>
public sealed class GetSiteAccessHandler : IQueryHandler<GetSiteAccess, SiteAccess>
{
    private readonly JpmsContext context;
    public GetSiteAccessHandler(JpmsContext context) { this.context = context; }

    public async Task<SiteAccess> HandleAsync(GetSiteAccess query, CancellationToken cancellationToken)
    {
        var active = await context.SiteAccessTokens.FirstOrDefaultAsync(
            token => token.ProjectId == query.ProjectId && token.IsActive, cancellationToken);
        if (active is not null) return new SiteAccess(query.ProjectId, active.Token);

        var minted = new SiteAccessTokenEntity
        {
            SiteAccessTokenId = LabourIdentifierFactory.NextSiteAccessTokenId(),
            ProjectId = query.ProjectId,
            Token = LabourIdentifierFactory.NextSiteToken(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        context.SiteAccessTokens.Add(minted);
        await context.SaveChangesAsync(cancellationToken);
        return new SiteAccess(query.ProjectId, minted.Token);
    }
}
