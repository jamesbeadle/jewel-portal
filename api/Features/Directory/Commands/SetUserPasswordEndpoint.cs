using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

/// <summary>POST /api/directory/password. SetByEmail is stamped from the resolved caller — never
/// trusted from the client — and the password never appears in an answer or an error.</summary>
public sealed class SetUserPasswordEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetUserPasswordAuthorisation authorisation;
    private readonly SetUserPasswordValidation validation;
    private readonly ICommandHandler<SetUserPassword, Acknowledgement> handler;

    public SetUserPasswordEndpoint(
        SignedInUserResolver users,
        SetUserPasswordAuthorisation authorisation,
        SetUserPasswordValidation validation,
        ICommandHandler<SetUserPassword, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetUserPassword))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "directory/password")] HttpRequest request)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<SetUserPassword>(cancellationToken);
        if (posted is null) return new BadRequestResult();

        var command = posted with { SetByEmail = signedInUser.Email };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(new[] { ex.Message });
        }
    }
}
