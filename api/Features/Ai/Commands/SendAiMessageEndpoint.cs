using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

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

    public SendAiMessageEndpoint(
        SignedInUserResolver users,
        AiCaller caller,
        SendAiMessageAuthorisation authorisation,
        SendAiMessageValidation validation,
        ICommandHandler<SendAiMessage, AiTurnResult> handler)
    {
        this.users = users;
        this.caller = caller;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
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
    }
}
