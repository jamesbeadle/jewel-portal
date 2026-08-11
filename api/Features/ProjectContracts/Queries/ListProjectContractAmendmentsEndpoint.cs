using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Queries;

/// <summary>
/// GET /api/projects/{projectId}/contract/amendments — the project's amendments in the order they
/// were made. An empty list is the common case, not an error.
/// </summary>
public sealed class ListProjectContractAmendmentsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListProjectContractAmendments, IReadOnlyList<ProjectContractAmendment>> handler;

    public ListProjectContractAmendmentsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListProjectContractAmendments, IReadOnlyList<ProjectContractAmendment>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListProjectContractAmendments))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/contract/amendments")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProjectContractRoles.AllowedToReadContract.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var amendments = await handler.HandleAsync(new ListProjectContractAmendments(projectId), cancellationToken);
        return new OkObjectResult(amendments);
    }
}
