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

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed class RotateSiteAccessTokenEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<RotateSiteAccessToken, SiteAccess> handler;
    public RotateSiteAccessTokenEndpoint(SignedInUserResolver users, ICommandHandler<RotateSiteAccessToken, SiteAccess> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(RotateSiteAccessToken))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/labour/site-access/rotation")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new RotateSiteAccessToken(projectId), request.HttpContext.RequestAborted));
    }
}

/// <summary>Deactivates all existing tokens for the project and mints a fresh one — printed or
/// leaked QR codes stop working the moment this returns.</summary>
public sealed class RotateSiteAccessTokenHandler : ICommandHandler<RotateSiteAccessToken, SiteAccess>
{
    private readonly JpmsContext context;
    public RotateSiteAccessTokenHandler(JpmsContext context) { this.context = context; }

    public async Task<SiteAccess> HandleAsync(RotateSiteAccessToken command, CancellationToken cancellationToken)
    {
        var existing = await context.SiteAccessTokens
            .Where(token => token.ProjectId == command.ProjectId && token.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var token in existing) token.IsActive = false;

        var minted = new SiteAccessTokenEntity
        {
            SiteAccessTokenId = LabourIdentifierFactory.NextSiteAccessTokenId(),
            ProjectId = command.ProjectId,
            Token = LabourIdentifierFactory.NextSiteToken(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        context.SiteAccessTokens.Add(minted);
        await context.SaveChangesAsync(cancellationToken);
        return new SiteAccess(command.ProjectId, minted.Token);
    }
}
