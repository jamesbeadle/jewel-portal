using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Clients.Queries;

public sealed class GetClientByIdEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetClientById, Client?> handler;

    public GetClientByIdEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetClientById, Client?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Client records (contact details) are internal-only reads.
    private static readonly RoleSet RolesThatMayReadClients = JpmsRoleSets.AllInternal;

    [Function(nameof(GetClientById))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "clients/{clientId}")] HttpRequest request,
        string clientId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadClients.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var client = await handler.HandleAsync(new GetClientById(clientId), request.HttpContext.RequestAborted);
        return new OkObjectResult(client);
    }
}
