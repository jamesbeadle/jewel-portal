using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Mobilisation.Queries;

public sealed class GetMobilisationChecklistForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetMobilisationChecklistForProject, MobilisationChecklist> handler;
    public GetMobilisationChecklistForProjectEndpoint(SignedInUserResolver users, IQueryHandler<GetMobilisationChecklistForProject, MobilisationChecklist> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(GetMobilisationChecklistForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/mobilisation")] HttpRequest request, string projectId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new GetMobilisationChecklistForProject(projectId), request.HttpContext.RequestAborted));
    }
}
