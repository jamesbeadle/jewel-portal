using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.UsefulInformation;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Queries;

public sealed class ListUsefulInformationForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListUsefulInformationForProject, IReadOnlyList<UsefulInformationNote>> handler;
    public ListUsefulInformationForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListUsefulInformationForProject, IReadOnlyList<UsefulInformationNote>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListUsefulInformationForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/useful-information")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        // Internal-only in both directions — the notes hold door codes and the like, so external
        // portal logins (subcontractor, and in future clients/architects) never pass this gate.
        if (!UsefulInformationRoles.AllowedToRead.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListUsefulInformationForProject(projectId), request.HttpContext.RequestAborted));
    }
}
