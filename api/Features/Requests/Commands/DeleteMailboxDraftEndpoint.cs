using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// POST /api/mailbox/drafts/delete — delete one unsent draft from the shared mailbox's Drafts
/// folder. JSON body: { "messageId": "..." } — the draft's mailbox message id, as the draft-
/// staging results return it (DraftMessageId). The Graph client verifies the message really is an
/// unsent draft before deleting, so sent or received mail can never be removed here; Graph moves
/// the draft to Deleted Items, so a mistaken delete is recoverable from Outlook for a while.
/// </summary>
public sealed class DeleteMailboxDraftEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteMailboxDraftAuthorisation authorisation;
    private readonly DeleteMailboxDraftValidation validation;
    private readonly ICommandHandler<DeleteMailboxDraft, Acknowledgement> handler;
    private readonly Audit.AuditActor auditActor;

    public DeleteMailboxDraftEndpoint(
        SignedInUserResolver users,
        DeleteMailboxDraftAuthorisation authorisation,
        DeleteMailboxDraftValidation validation,
        ICommandHandler<DeleteMailboxDraft, Acknowledgement> handler,
        Audit.AuditActor auditActor)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
        this.auditActor = auditActor;
    }

    [Function(nameof(DeleteMailboxDraft))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/drafts/delete")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        // Attribute the handler's MailboxDraftDeleted audit row to whoever pressed the button.
        auditActor.Email = signedInUser.Email;

        DeleteMailboxDraft? body = null;
        try { body = await request.ReadFromJsonAsync<DeleteMailboxDraft>(); }
        catch { /* validation reports the missing message id */ }
        var command = new DeleteMailboxDraft(body?.MessageId ?? "");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // Not-a-draft / already-gone / mailbox-down are user-fixable answers — surface verbatim.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
