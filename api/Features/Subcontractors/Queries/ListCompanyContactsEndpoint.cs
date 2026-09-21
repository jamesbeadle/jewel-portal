using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListCompanyContactsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCompanyContacts, IReadOnlyList<CompanyContact>> handler;

    public ListCompanyContactsEndpoint(SignedInUserResolver users, IQueryHandler<ListCompanyContacts, IReadOnlyList<CompanyContact>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListCompanyContacts))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subcontractors/{subcontractorId}/contacts")] HttpRequest request,
        string subcontractorId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!DirectoryRoles.AllowedToList.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListCompanyContacts(subcontractorId), request.HttpContext.RequestAborted));
    }
}
