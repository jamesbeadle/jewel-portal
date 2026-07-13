using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Clients.Queries;

public sealed class ListClientsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListClients, IReadOnlyList<Client>> handler;

    public ListClientsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListClients, IReadOnlyList<Client>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // The client directory (contact details) is an internal-only read.
    private static readonly RoleSet RolesThatMayReadClients = JpmsRoleSets.AllInternal;

    [Function(nameof(ListClients))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "clients")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadClients.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var clients = await handler.HandleAsync(new ListClients(), request.HttpContext.RequestAborted);
        return new OkObjectResult(clients);
    }
}
