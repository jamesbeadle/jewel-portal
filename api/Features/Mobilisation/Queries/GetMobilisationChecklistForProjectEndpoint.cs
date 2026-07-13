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

    // Mobilisation checklists are internal-only reads; updating items is gated separately
    // by UpdateMobilisationChecklistItemAuthorisation.
    private static readonly RoleSet RolesThatMayReadMobilisation = JpmsRoleSets.AllInternal;

    [Function(nameof(GetMobilisationChecklistForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/mobilisation")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadMobilisation.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new GetMobilisationChecklistForProject(projectId), request.HttpContext.RequestAborted));
    }
}
