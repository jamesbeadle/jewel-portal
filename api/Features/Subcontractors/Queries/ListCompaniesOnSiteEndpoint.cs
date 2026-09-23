using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

/// <summary>The companies on site, read by the compliance register beside the documents — the same
/// gate as the documents themselves.</summary>
public sealed class ListCompaniesOnSiteEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCompaniesOnSite, IReadOnlyList<CompanyOnSite>> handler;

    public ListCompaniesOnSiteEndpoint(SignedInUserResolver users, IQueryHandler<ListCompaniesOnSite, IReadOnlyList<CompanyOnSite>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListCompaniesOnSite))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "companies-on-site")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!DirectoryRoles.AllowedToReadCompliance.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return new OkObjectResult(await handler.HandleAsync(new ListCompaniesOnSite(), request.HttpContext.RequestAborted));
    }
}
