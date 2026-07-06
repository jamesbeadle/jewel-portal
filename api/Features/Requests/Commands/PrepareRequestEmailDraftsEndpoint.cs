using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// POST /api/requests/email-drafts — create one Outlook draft in the projects mailbox per request
/// id in the JSON body { "requestIds": ["...", "..."] }. The response reports per-request
/// outcomes; a request that can't be drafted (no resolvable recipient, unknown id) doesn't stop
/// the others. Nothing is sent — every draft waits in the mailbox's Drafts folder.
/// </summary>
public sealed class PrepareRequestEmailDraftsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PrepareRequestEmailDraftsAuthorisation authorisation;
    private readonly PrepareRequestEmailDraftsValidation validation;
    private readonly ICommandHandler<PrepareRequestEmailDrafts, RequestEmailDraftBatch> handler;

    public PrepareRequestEmailDraftsEndpoint(
        SignedInUserResolver users,
        PrepareRequestEmailDraftsAuthorisation authorisation,
        PrepareRequestEmailDraftsValidation validation,
        ICommandHandler<PrepareRequestEmailDrafts, RequestEmailDraftBatch> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(PrepareRequestEmailDrafts))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/email-drafts")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        PrepareRequestEmailDrafts? command = null;
        try { command = await request.ReadFromJsonAsync<PrepareRequestEmailDrafts>(); }
        catch { /* a malformed body fails validation below */ }
        if (command is null) return new BadRequestObjectResult("A JSON body with requestIds is required.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // Per-request failures are reported inside the batch; reaching here means something
            // run-wide and user-fixable (e.g. the mailbox connection) — surface it verbatim.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
