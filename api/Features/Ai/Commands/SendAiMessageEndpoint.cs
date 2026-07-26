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
/// POST /api/ai/messages — one user turn.
///
/// <para>The client sends only the new message, the conversation id and the scope it was sent from.
/// It never sends history: the server rebuilds the transcript from the database, which is what makes
/// the stored conversation a record of what the model actually saw.</para>
/// </summary>
public sealed class SendAiMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AiCaller caller;
    private readonly SendAiMessageAuthorisation authorisation;
    private readonly SendAiMessageValidation validation;
    private readonly ICommandHandler<SendAiMessage, AiTurnResult> handler;
    private readonly ILogger<SendAiMessageEndpoint> logger;

    public SendAiMessageEndpoint(
        SignedInUserResolver users,
        AiCaller caller,
        SendAiMessageAuthorisation authorisation,
        SendAiMessageValidation validation,
        ICommandHandler<SendAiMessage, AiTurnResult> handler,
        ILogger<SendAiMessageEndpoint> logger)
    {
        this.users = users;
        this.caller = caller;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
        this.logger = logger;
    }

    [Function(nameof(SendAiMessage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ai/messages")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        // The tool layer needs the caller's roles, not just an email. Set immediately after the gate.
        caller.Current = signedInUser;

        var body = await request.ReadFromJsonAsync<SendAiMessage>();
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
            // Deliberately verbose. This endpoint is reachable only by administrators and directors,
            // and an opaque 500 in a chat panel is a debugging session — the sentence below is the
            // difference between "Backend call failure" and knowing a migration has not been run.
            logger.LogError(ex, "Assistant turn failed for {Email}.", signedInUser.Email);

            return new ObjectResult(Explain(ex))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    /// <summary>
    /// Turns the exception into something the person reading the chat panel can act on. Recognises
    /// the failures that are configuration rather than bugs; everything else falls back to the
    /// exception's own message, which is more use than nothing.
    /// </summary>
    private static string Explain(Exception ex)
    {
        var message = ex.Message;

        if (message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase))
        {
            return "The assistant's database tables are missing or out of date. "
                + "A pending EF migration has not been applied to this environment. "
                + $"({message})";
        }

        if (message.Contains("Unable to resolve service", StringComparison.OrdinalIgnoreCase))
        {
            return $"The assistant is not wired up correctly on this environment. ({message})";
        }

        return $"The assistant hit an unexpected error. ({message})";
    }
}
