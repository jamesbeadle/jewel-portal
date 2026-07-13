using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

public sealed class ListWorkersEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListWorkers, IReadOnlyList<Worker>> handler;
    public ListWorkersEndpoint(SignedInUserResolver users, IQueryHandler<ListWorkers, IReadOnlyList<Worker>> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ListWorkers))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "labour/workers")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        // Rates ride on this response, so the whole registry is gated to the managing roles.
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListWorkers(), request.HttpContext.RequestAborted));
    }
}

public sealed class ListWorkersHandler : IQueryHandler<ListWorkers, IReadOnlyList<Worker>>
{
    private readonly JpmsContext context;
    public ListWorkersHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Worker>> HandleAsync(ListWorkers query, CancellationToken cancellationToken)
    {
        var workers = await context.Workers.OrderBy(worker => worker.Name).ToListAsync(cancellationToken);
        return workers.Select(worker => worker.ToModel()).ToList();
    }
}
