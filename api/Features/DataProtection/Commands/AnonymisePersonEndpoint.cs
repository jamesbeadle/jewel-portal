using Jewel.JPMS.Contracts.DataProtection;

namespace Jewel.JPMS.Api.Features.DataProtection.Commands;

/// <summary>POST /api/admin/people/anonymise — erase a person's details and pseudonymise their trail.</summary>
public sealed class AnonymisePersonEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AnonymisePersonAuthorisation authorisation;
    private readonly AnonymisePersonValidation validation;
    private readonly ICommandHandler<AnonymisePerson, PersonAnonymisation> handler;

    public AnonymisePersonEndpoint(
        SignedInUserResolver users, AnonymisePersonAuthorisation authorisation,
        AnonymisePersonValidation validation, ICommandHandler<AnonymisePerson, PersonAnonymisation> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(AnonymisePerson))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/people/anonymise")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<AnonymisePerson>();
        if (body is null) return new BadRequestResult();
        var command = body with { AnonymisedByEmail = signedInUser.Email };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException refusal)
        {
            return new BadRequestObjectResult(new[] { refusal.Message });
        }
    }
}
