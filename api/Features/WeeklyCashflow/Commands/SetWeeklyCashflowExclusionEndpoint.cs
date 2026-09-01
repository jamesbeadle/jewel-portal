using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class SetWeeklyCashflowExclusionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetWeeklyCashflowExclusionAuthorisation authorisation;
    private readonly SetWeeklyCashflowExclusionValidation validation;
    private readonly ICommandHandler<SetWeeklyCashflowExclusion, WeeklyCashflowExclusionAnswer> handler;

    public SetWeeklyCashflowExclusionEndpoint(
        SignedInUserResolver users,
        SetWeeklyCashflowExclusionAuthorisation authorisation,
        SetWeeklyCashflowExclusionValidation validation,
        ICommandHandler<SetWeeklyCashflowExclusion, WeeklyCashflowExclusionAnswer> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetWeeklyCashflowExclusion))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "weekly-cashflow/exclusions")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<SetWeeklyCashflowExclusion>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("An exclusion body is required.");
        var command = posted with { ExcludedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
