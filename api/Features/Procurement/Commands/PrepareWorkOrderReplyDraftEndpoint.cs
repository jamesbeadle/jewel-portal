using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// POST /api/work-orders/{workOrderId}/draft-email/reply — create an Outlook draft REPLY (in the
/// original conversation thread) to an email linked to the work order, carrying the rendered
/// purchase-order PDF. JSON body: { "mailboxMessageId": "...", "htmlCoverNote": "..." } — the Graph
/// id of the conversation email to reply to, and the note placed above the quoted history.
/// Nothing is sent — the draft waits in the mailbox's Drafts folder.
/// </summary>
public sealed class PrepareWorkOrderReplyDraftEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PrepareWorkOrderReplyDraftAuthorisation authorisation;
    private readonly PrepareWorkOrderReplyDraftValidation validation;
    private readonly ICommandHandler<PrepareWorkOrderReplyDraft, WorkOrderReplyDraft> handler;
    private readonly Audit.AuditActor auditActor;

    public PrepareWorkOrderReplyDraftEndpoint(
        SignedInUserResolver users,
        PrepareWorkOrderReplyDraftAuthorisation authorisation,
        PrepareWorkOrderReplyDraftValidation validation,
        ICommandHandler<PrepareWorkOrderReplyDraft, WorkOrderReplyDraft> handler,
        Audit.AuditActor auditActor)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
        this.auditActor = auditActor;
    }

    [Function(nameof(PrepareWorkOrderReplyDraft))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "work-orders/{workOrderId}/draft-email/reply")] HttpRequest request,
        string workOrderId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        // Attribute the handler's DraftCreated audit row to whoever pressed the button.
        auditActor.Email = signedInUser.Email;

        PrepareWorkOrderReplyDraft? body = null;
        try { body = await request.ReadFromJsonAsync<PrepareWorkOrderReplyDraft>(); }
        catch { /* validation reports the missing fields */ }
        var command = new PrepareWorkOrderReplyDraft(workOrderId, body?.MailboxMessageId ?? "", body?.HtmlCoverNote ?? "");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // A vanished original email / draft-status order / unconfigured mailbox are
            // user-fixable — surface verbatim.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
