using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// One-off admin sweep: POST mailbox/retag-requests moves historic request mail from legacy flat tags
/// onto project-qualified ones. Gated to the triage roles (the same people who own the mailbox views).
/// Safe to re-run; returns the sweep summary.
/// </summary>
public sealed class RetagRequestWorkflowTagsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<RetagRequestWorkflowTags, RequestRetagSummary> handler;
    public RetagRequestWorkflowTagsEndpoint(SignedInUserResolver users, ICommandHandler<RetagRequestWorkflowTags, RequestRetagSummary> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(RetagRequestWorkflowTags))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/retag-requests")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        return new OkObjectResult(await handler.HandleAsync(new RetagRequestWorkflowTags(), request.HttpContext.RequestAborted));
    }
}
