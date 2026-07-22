using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Clients.Commands;

/// <summary>PUT /api/clients/{clientId}/contact — update the client's name / primary contact.</summary>
public sealed class UpdateClientContactEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateClientContactAuthorisation authorisation;
    private readonly UpdateClientContactValidation validation;
    private readonly ICommandHandler<UpdateClientContact, Client> handler;

    public UpdateClientContactEndpoint(
        SignedInUserResolver users,
        UpdateClientContactAuthorisation authorisation,
        UpdateClientContactValidation validation,
        ICommandHandler<UpdateClientContact, Client> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateClientContact))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "clients/{clientId}/contact")] HttpRequest request,
        string clientId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<UpdateClientContact>();
        if (body is null) return new BadRequestResult();

        var command = body with { ClientId = clientId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
