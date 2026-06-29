using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListDiscardedIntakeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListDiscardedIntake, PagedResult<IntakeEmail>> handler;
    public ListDiscardedIntakeEndpoint(SignedInUserResolver users, IQueryHandler<ListDiscardedIntake, PagedResult<IntakeEmail>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListDiscardedIntake))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "intake/discarded")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var skip = int.TryParse(request.Query["skip"], out var s) ? s : 0;
        var take = int.TryParse(request.Query["take"], out var t) ? t : 25;
        return new OkObjectResult(await handler.HandleAsync(new ListDiscardedIntake(skip, take), request.HttpContext.RequestAborted));
    }
}
