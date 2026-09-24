using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListRequestMessagesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListRequestMessages, IReadOnlyList<RequestMessage>> handler;
    public ListRequestMessagesEndpoint(SignedInUserResolver users, IQueryHandler<ListRequestMessages, IReadOnlyList<RequestMessage>> handler) { this.users = users; this.handler = handler; }

    // Internal only: a request carries the business's notes and mail (2026-09-24).
    private static readonly RoleSet RolesThatMayReadRequests = JpmsRoleSets.ProjectDeliveryTeam;

    [Function(nameof(ListRequestMessages))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/messages")] HttpRequest request, string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRequests.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListRequestMessages(requestId), request.HttpContext.RequestAborted));
    }
}
