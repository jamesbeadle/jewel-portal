using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Ai.Commands;

/// <summary>
/// POST /api/ai/turn/continue — the next hop of a turn already in flight.
///
/// <para>Carries no message: everything the model needs is already in the transcript, server-side.
/// The client is only driving the pump.</para>
/// </summary>
public sealed class ContinueAiTurnEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AiCaller caller;
    private readonly ContinueAiTurnAuthorisation authorisation;
    private readonly ContinueAiTurnValidation validation;
    private readonly ICommandHandler<ContinueAiTurn, AiTurnResult> handler;
    private readonly ILogger<ContinueAiTurnEndpoint> logger;

    public ContinueAiTurnEndpoint(
        SignedInUserResolver users,
        AiCaller caller,
        ContinueAiTurnAuthorisation authorisation,
        ContinueAiTurnValidation validation,
        ICommandHandler<ContinueAiTurn, AiTurnResult> handler,
        ILogger<ContinueAiTurnEndpoint> logger)
    {
        this.users = users;
        this.caller = caller;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
        this.logger = logger;
    }

    [Function(nameof(ContinueAiTurn))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ai/turn/continue")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        caller.Current = signedInUser;

        var body = await request.ReadFromJsonAsync<ContinueAiTurn>();
        if (body is null) return new BadRequestResult();

        var command = body with { SentByEmail = signedInUser.Email };

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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Assistant hop failed for {Email}.", signedInUser.Email);
            return new ObjectResult($"The assistant hit an unexpected error. ({ex.Message})")
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
