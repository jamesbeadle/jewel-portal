using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Ai.Commands;

/// <summary>
/// POST /api/ai/attachments — a file for the assistant to read. No Claude call happens here (the
/// upload is free; the user's next message is where the model reads it), so the gate is simply the
/// assistant's own role set. Bytes travel base64 in the JSON body and are discarded after
/// extraction — see AddAiAttachmentHandler.
/// </summary>
public sealed class AddAiAttachmentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AiCaller caller;
    private readonly AddAiAttachmentValidation validation;
    private readonly ICommandHandler<AddAiAttachment, AiAttachmentReceipt> handler;
    private readonly ILogger<AddAiAttachmentEndpoint> logger;

    public AddAiAttachmentEndpoint(
        SignedInUserResolver users,
        AiCaller caller,
        AddAiAttachmentValidation validation,
        ICommandHandler<AddAiAttachment, AiAttachmentReceipt> handler,
        ILogger<AddAiAttachmentEndpoint> logger)
    {
        this.users = users;
        this.caller = caller;
        this.validation = validation;
        this.handler = handler;
        this.logger = logger;
    }

    [Function(nameof(AddAiAttachment))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ai/attachments")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AiRoles.AllowedToUseAssistant.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        caller.Current = signedInUser;

        var body = await ReadBody(request);
        if (body is null) return new BadRequestResult();

        var command = body with { SentByEmail = signedInUser.Email };

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // The handler's own refusals — wrong format, too big, unreadable — are sentences the
            // panel shows verbatim, so they travel as a 400 with the message.
            return new BadRequestObjectResult(new[] { ex.Message });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Chat attachment failed for {Email}.", signedInUser.Email);
            return new ObjectResult($"The attachment could not be read. ({ex.Message})")
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    private static async Task<AddAiAttachment?> ReadBody(HttpRequest request)
    {
        try { return await request.ReadFromJsonAsync<AddAiAttachment>(); }
        catch { return null; }
    }
}
