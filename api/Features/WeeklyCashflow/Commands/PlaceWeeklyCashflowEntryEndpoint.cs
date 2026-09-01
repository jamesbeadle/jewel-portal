using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class PlaceWeeklyCashflowEntryEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PlaceWeeklyCashflowEntryAuthorisation authorisation;
    private readonly PlaceWeeklyCashflowEntryValidation validation;
    private readonly ICommandHandler<PlaceWeeklyCashflowEntry, WeeklyCashflowPlacementAnswer> handler;

    public PlaceWeeklyCashflowEntryEndpoint(
        SignedInUserResolver users,
        PlaceWeeklyCashflowEntryAuthorisation authorisation,
        PlaceWeeklyCashflowEntryValidation validation,
        ICommandHandler<PlaceWeeklyCashflowEntry, WeeklyCashflowPlacementAnswer> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(PlaceWeeklyCashflowEntry))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "weekly-cashflow/placements")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<PlaceWeeklyCashflowEntry>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A placement body is required.");
        var command = posted with { MovedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
