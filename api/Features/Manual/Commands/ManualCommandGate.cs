using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Api.Features.Manual.Commands;

/// <summary>
/// The gate every manual endpoint passes a command through, in order: the signed-in user, the
/// stamp of their email onto the command, authorisation, validation, then the handler. A refusal
/// the handler raises about the module's state comes back as a 400 with its sentence.
/// </summary>
public sealed class ManualCommandGate
{
    private readonly SignedInUserResolver users;

    public ManualCommandGate(SignedInUserResolver users) { this.users = users; }

    public async Task<IActionResult> RunAsync<TCommand, TResult>(
        HttpRequest request,
        Func<TCommand, SignedInUser, TCommand> stamp,
        Func<SignedInUser, TCommand, bool> allows,
        Func<TCommand, ValidationOutcome> check,
        ICommandHandler<TCommand, TResult> handler)
        where TCommand : class, ICommand<TResult>
    {
        var cancellationToken = ManualEndpointRequest.CancellationOf(request);
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<TCommand>(cancellationToken);
        if (posted is null) return new BadRequestResult();

        var command = stamp(posted, signedInUser);
        if (!allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var outcome = check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException refusal)
        {
            return new BadRequestObjectResult(new[] { refusal.Message });
        }
    }
}
