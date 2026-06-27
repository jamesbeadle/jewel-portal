using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class GetIntakeEmailDetailEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetIntakeEmailDetail, IntakeEmailDetail> handler;
    public GetIntakeEmailDetailEndpoint(SignedInUserResolver users, IQueryHandler<GetIntakeEmailDetail, IntakeEmailDetail> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(GetIntakeEmailDetail))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "intake/{intakeId}/detail")] HttpRequest request,
        string intakeId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new GetIntakeEmailDetail(intakeId), request.HttpContext.RequestAborted));
    }
}
