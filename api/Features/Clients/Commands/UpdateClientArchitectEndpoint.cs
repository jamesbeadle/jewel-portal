using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Clients.Commands;

/// <summary>PUT /api/clients/{clientId}/architect — update the client's architect / primary contact.</summary>
public sealed class UpdateClientArchitectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateClientArchitectAuthorisation authorisation;
    private readonly UpdateClientArchitectValidation validation;
    private readonly ICommandHandler<UpdateClientArchitect, Client> handler;

    public UpdateClientArchitectEndpoint(
        SignedInUserResolver users,
        UpdateClientArchitectAuthorisation authorisation,
        UpdateClientArchitectValidation validation,
        ICommandHandler<UpdateClientArchitect, Client> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateClientArchitect))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "clients/{clientId}/architect")] HttpRequest request,
        string clientId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<UpdateClientArchitect>();
        if (body is null) return new BadRequestResult();

        var command = body with { ClientId = clientId };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
