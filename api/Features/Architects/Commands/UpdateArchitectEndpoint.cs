using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Architects.Commands;

/// <summary>PUT /api/architects/{architectId} — update an architect practice's name / contact.</summary>
public sealed class UpdateArchitectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateArchitectAuthorisation authorisation;
    private readonly UpdateArchitectValidation validation;
    private readonly ICommandHandler<UpdateArchitect, Architect> handler;

    public UpdateArchitectEndpoint(
        SignedInUserResolver users,
        UpdateArchitectAuthorisation authorisation,
        UpdateArchitectValidation validation,
        ICommandHandler<UpdateArchitect, Architect> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateArchitect))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "architects/{architectId}")] HttpRequest request,
        string architectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<UpdateArchitect>();
        if (body is null) return new BadRequestResult();

        var command = body with { ArchitectId = architectId };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
