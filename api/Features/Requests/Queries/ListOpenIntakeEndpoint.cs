using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListOpenIntakeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListOpenIntake, IReadOnlyList<IntakeEmail>> handler;
    public ListOpenIntakeEndpoint(SignedInUserResolver users, IQueryHandler<ListOpenIntake, IReadOnlyList<IntakeEmail>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListOpenIntake))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "intake")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListOpenIntake(), request.HttpContext.RequestAborted));
    }
}
