using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class CreateWeeklyCashflowItemEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateWeeklyCashflowItemAuthorisation authorisation;
    private readonly CreateWeeklyCashflowItemValidation validation;
    private readonly ICommandHandler<CreateWeeklyCashflowItem, WeeklyCashflowItem> handler;

    public CreateWeeklyCashflowItemEndpoint(
        SignedInUserResolver users,
        CreateWeeklyCashflowItemAuthorisation authorisation,
        CreateWeeklyCashflowItemValidation validation,
        ICommandHandler<CreateWeeklyCashflowItem, WeeklyCashflowItem> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateWeeklyCashflowItem))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "weekly-cashflow/items")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<CreateWeeklyCashflowItem>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("An item body is required.");
        var command = posted with { CreatedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
