using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// One-off admin sweep: POST mailbox/retag-work-orders moves historic work-order mail from legacy flat
/// tags onto project-qualified ones. Safe to re-run; returns the sweep summary, including the threads
/// it left for a person because two projects' orders share the number.
/// </summary>
public sealed class RetagWorkOrderWorkflowTagsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RetagWorkOrderWorkflowTagsAuthorisation authorisation;
    private readonly ICommandHandler<RetagWorkOrderWorkflowTags, WorkOrderRetagSummary> handler;

    public RetagWorkOrderWorkflowTagsEndpoint(
        SignedInUserResolver users,
        RetagWorkOrderWorkflowTagsAuthorisation authorisation,
        ICommandHandler<RetagWorkOrderWorkflowTags, WorkOrderRetagSummary> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(RetagWorkOrderWorkflowTags))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/retag-work-orders")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new RetagWorkOrderWorkflowTags();
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
