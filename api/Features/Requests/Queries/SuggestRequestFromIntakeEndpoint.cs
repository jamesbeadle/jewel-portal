using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class SuggestRequestFromIntakeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<SuggestRequestFromIntake, RequestSuggestion> handler;
    public SuggestRequestFromIntakeEndpoint(SignedInUserResolver users, IQueryHandler<SuggestRequestFromIntake, RequestSuggestion> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(SuggestRequestFromIntake))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "intake/{intakeId}/suggest")] HttpRequest request,
        string intakeId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new SuggestRequestFromIntake(intakeId), request.HttpContext.RequestAborted));
    }
}
