using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Projects.Contacts;

public sealed class ListProjectContactsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListProjectContacts, IReadOnlyList<ProjectContact>> handler;

    public ListProjectContactsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListProjectContacts, IReadOnlyList<ProjectContact>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Reading the contact sheet is open to all internal staff; managing contacts is gated
    // separately by ProjectContactAuthorisation.
    private static readonly RoleSet RolesThatMayReadContacts = JpmsRoleSets.AllInternal;

    [Function(nameof(ListProjectContacts))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/contacts")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadContacts.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var contacts = await handler.HandleAsync(new ListProjectContacts(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(contacts);
    }
}
