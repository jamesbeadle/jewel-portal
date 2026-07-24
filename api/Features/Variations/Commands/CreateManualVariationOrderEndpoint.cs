using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/projects/{projectId}/manual-variation — create a standalone variation order (in
/// Quoting) with no request behind it. Body: { title, description?, estimatedValue?, number? }.
/// The creator is the signed-in user. ProjectId is taken from the route.
/// </summary>
public sealed class CreateManualVariationOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateManualVariationOrderAuthorisation authorisation;
    private readonly CreateManualVariationOrderValidation validation;
    private readonly ICommandHandler<CreateManualVariationOrder, VariationOrder> handler;

    public CreateManualVariationOrderEndpoint(
        SignedInUserResolver users,
        CreateManualVariationOrderAuthorisation authorisation,
        CreateManualVariationOrderValidation validation,
        ICommandHandler<CreateManualVariationOrder, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateManualVariationOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/manual-variation")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        CreateManualVariationOrder? body = null;
        try { body = await request.ReadFromJsonAsync<CreateManualVariationOrder>(cancellationToken); }
        catch (System.Text.Json.JsonException) { }
        if (body is null) return new BadRequestResult();

        var command = body with { ProjectId = projectId, CreatedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
