using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListSubcontractorsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListSubcontractors, IReadOnlyList<Subcontractor>> handler;

    public ListSubcontractorsEndpoint(SignedInUserResolver users, IQueryHandler<ListSubcontractors, IReadOnlyList<Subcontractor>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListSubcontractors))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subcontractors")] HttpRequest request)
    {
        if (users.Resolve(request) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListSubcontractors(), request.HttpContext.RequestAborted));
    }
}
