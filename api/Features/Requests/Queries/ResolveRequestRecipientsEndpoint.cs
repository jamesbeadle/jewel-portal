using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ResolveRequestRecipientsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ResolveRequestRecipients, RequestRecipientSet> handler;

    public ResolveRequestRecipientsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ResolveRequestRecipients, RequestRecipientSet> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Internal only: who a request goes to is the office's address book (2026-09-24).
    private static readonly RoleSet RolesThatMayReadRequests = JpmsRoleSets.ProjectDeliveryTeam;

    [Function(nameof(ResolveRequestRecipients))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/recipients")] HttpRequest request,
        string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRequests.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var recipients = await handler.HandleAsync(new ResolveRequestRecipients(requestId), request.HttpContext.RequestAborted);
        return new OkObjectResult(recipients);
    }
}
