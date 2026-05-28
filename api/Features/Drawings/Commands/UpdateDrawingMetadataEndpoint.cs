using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class UpdateDrawingMetadataEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateDrawingMetadataAuthorisation authorisation;
    private readonly UpdateDrawingMetadataValidation validation;
    private readonly ICommandHandler<UpdateDrawingMetadata, Drawing> handler;

    public UpdateDrawingMetadataEndpoint(
        SignedInUserResolver users,
        UpdateDrawingMetadataAuthorisation authorisation,
        UpdateDrawingMetadataValidation validation,
        ICommandHandler<UpdateDrawingMetadata, Drawing> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateDrawingMetadata))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "drawings/{drawingId}")] HttpRequest request,
        string drawingId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UpdateDrawingMetadata>();
        if (command is null) return new BadRequestResult();
        if (command.DrawingId != drawingId) return new BadRequestObjectResult("Route drawingId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var drawing = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(drawing);
    }
}
