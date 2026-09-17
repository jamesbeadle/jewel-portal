using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// POST /api/requests/{requestId}/email-draft/reply — create an Outlook draft REPLY (in the original
/// conversation thread) to an email linked to the request, carrying the official document PDF.
/// JSON body: { "mailboxMessageId": "..." } — the Graph id of the conversation email to reply to.
/// Nothing is sent — the draft waits in the mailbox's Drafts folder.
/// </summary>
public sealed class SendRequestReplyEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SendRequestReplyAuthorisation authorisation;
    private readonly SendRequestReplyValidation validation;
    private readonly ICommandHandler<SendRequestReply, RequestEmailOutcome> handler;
    private readonly Audit.AuditActor auditActor;

    public SendRequestReplyEndpoint(
        SignedInUserResolver users,
        SendRequestReplyAuthorisation authorisation,
        SendRequestReplyValidation validation,
        ICommandHandler<SendRequestReply, RequestEmailOutcome> handler,
        Audit.AuditActor auditActor)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
        this.auditActor = auditActor;
    }

    [Function(nameof(SendRequestReply))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/email-draft/reply")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        // Attribute the handler's DraftCreated audit row to whoever pressed the button.
        auditActor.Email = signedInUser.Email;

        SendRequestReply? body = null;
        try { body = await request.ReadFromJsonAsync<SendRequestReply>(); }
        catch { /* validation reports the missing message id */ }
        var command = new SendRequestReply(requestId, body?.MailboxMessageId ?? "");

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
