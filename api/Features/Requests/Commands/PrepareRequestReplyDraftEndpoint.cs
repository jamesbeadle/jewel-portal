using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// POST /api/requests/{requestId}/email-draft/reply — create an Outlook draft REPLY (in the original
/// conversation thread) to an email linked to the request, carrying the official document PDF.
/// JSON body: { "mailboxMessageId": "..." } — the Graph id of the conversation email to reply to.
/// Nothing is sent — the draft waits in the mailbox's Drafts folder.
/// </summary>
public sealed class PrepareRequestReplyDraftEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PrepareRequestReplyDraftAuthorisation authorisation;
    private readonly PrepareRequestReplyDraftValidation validation;
    private readonly ICommandHandler<PrepareRequestReplyDraft, RequestEmailDraft> handler;

    public PrepareRequestReplyDraftEndpoint(
        SignedInUserResolver users,
        PrepareRequestReplyDraftAuthorisation authorisation,
        PrepareRequestReplyDraftValidation validation,
        ICommandHandler<PrepareRequestReplyDraft, RequestEmailDraft> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(PrepareRequestReplyDraft))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/email-draft/reply")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        PrepareRequestReplyDraft? body = null;
        try { body = await request.ReadFromJsonAsync<PrepareRequestReplyDraft>(); }
        catch { /* validation reports the missing message id */ }
        var command = new PrepareRequestReplyDraft(requestId, body?.MailboxMessageId ?? "");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // A vanished original email / unconfigured mailbox are user-fixable — surface verbatim.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
