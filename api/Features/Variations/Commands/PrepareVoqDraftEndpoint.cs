using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/requests/{requestId}/voq/draft — draft a VOQ from the request and its tagged emails
/// via the LLM. Nothing is saved; the proposal goes back for human review. No request body.
/// </summary>
public sealed class PrepareVoqDraftEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PrepareVoqDraftAuthorisation authorisation;
    private readonly PrepareVoqDraftValidation validation;
    private readonly ICommandHandler<PrepareVoqDraft, VoqDraftProposal> handler;

    public PrepareVoqDraftEndpoint(
        SignedInUserResolver users,
        PrepareVoqDraftAuthorisation authorisation,
        PrepareVoqDraftValidation validation,
        ICommandHandler<PrepareVoqDraft, VoqDraftProposal> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(PrepareVoqDraft))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/voq/draft")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new PrepareVoqDraft(requestId);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
