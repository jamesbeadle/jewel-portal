using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Ai.Commands;

/// <summary>
/// POST /api/ai/turn/collect — collects a reply that outlived its request's inline wait
/// (docs/ai/07-reply-collection.md).
///
/// <para>Carries the reply id and nothing else the model needs: the answer is on the server, and
/// this request either applies it as the hop it belongs to or says it is still pending.</para>
/// </summary>
public sealed class CollectAiReplyEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AiCaller caller;
    private readonly CollectAiReplyAuthorisation authorisation;
    private readonly CollectAiReplyValidation validation;
    private readonly ICommandHandler<CollectAiReply, AiTurnResult> handler;
    private readonly ILogger<CollectAiReplyEndpoint> logger;

    public CollectAiReplyEndpoint(
        SignedInUserResolver users,
        AiCaller caller,
        CollectAiReplyAuthorisation authorisation,
        CollectAiReplyValidation validation,
        ICommandHandler<CollectAiReply, AiTurnResult> handler,
        ILogger<CollectAiReplyEndpoint> logger)
    {
        this.users = users;
        this.caller = caller;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
        this.logger = logger;
    }

    [Function(nameof(CollectAiReply))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ai/turn/collect")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        caller.Current = signedInUser;

        var body = await request.ReadFromJsonAsync<CollectAiReply>();
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
        catch (DbUpdateConcurrencyException)
        {
            // Two collects raced for the same answer; the other one applied it. The transcript
            // holds the hop — the panel's Retry continues from there.
            return new BadRequestObjectResult(new[] { "That reply was already collected." });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Assistant reply collect failed for {Email}.", signedInUser.Email);
            return new ObjectResult(AiEndpointErrors.Explain(ex))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
