using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// POST /api/requests/{requestId}/email-draft — create an Outlook draft in the projects mailbox
/// carrying the official document PDF. Optional JSON body { "recipientOverride": "someone@x.com" }
/// addresses the draft to one ad-hoc email instead of the resolved client / architect preference.
/// Nothing is sent — the draft waits in the mailbox's Drafts folder.
/// </summary>
public sealed class PrepareRequestEmailDraftEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PrepareRequestEmailDraftAuthorisation authorisation;
    private readonly PrepareRequestEmailDraftValidation validation;
    private readonly ICommandHandler<PrepareRequestEmailDraft, RequestEmailDraft> handler;

    public PrepareRequestEmailDraftEndpoint(
        SignedInUserResolver users,
        PrepareRequestEmailDraftAuthorisation authorisation,
        PrepareRequestEmailDraftValidation validation,
        ICommandHandler<PrepareRequestEmailDraft, RequestEmailDraft> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(PrepareRequestEmailDraft))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/email-draft")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        PrepareRequestEmailDraft? body = null;
        if (request.ContentLength > 0)
        {
            try { body = await request.ReadFromJsonAsync<PrepareRequestEmailDraft>(); }
            catch { /* an empty or non-JSON body means "no override" */ }
        }
        var command = new PrepareRequestEmailDraft(requestId, body?.RecipientOverride);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // Missing recipients / unconfigured mailbox are user-fixable — surface the message verbatim.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
