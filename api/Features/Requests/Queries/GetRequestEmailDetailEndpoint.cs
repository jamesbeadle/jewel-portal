using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class GetRequestEmailDetailEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetRequestEmailDetail, MailboxMessageDetail> handler;
    public GetRequestEmailDetailEndpoint(SignedInUserResolver users, IQueryHandler<GetRequestEmailDetail, MailboxMessageDetail> handler) { this.users = users; this.handler = handler; }

    // Request reads are internal plus the architect, who reads/approves RFIs per the permissions matrix.
    // The request's own mail, not the request: read by the internal team alone (Nigel, 2026-09-19).
    // The rest of a request is JpmsRoleSets.InternalAndArchitect because the architect answers the
    // RFI — but the correspondence behind it is the business's, and an architect account reached it
    // on every project, not only their own.
    private static readonly RoleSet RolesThatMayReadRequestMail = JpmsRoleSets.AllInternal;

    // The message id travels in the query string, not the path (Graph ids contain path-unsafe chars).
    // Same gate as reading the conversation; the handler enforces that the message is
    // actually tagged to the request before returning any content.
    [Function(nameof(GetRequestEmailDetail))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/messages/email-detail")] HttpRequest request, string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRequestMail.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var id = request.Query["id"].ToString();
        if (string.IsNullOrWhiteSpace(id)) return new BadRequestObjectResult("id is required.");
        var imid = request.Query["imid"].ToString();
        var query = new GetRequestEmailDetail(requestId, id, string.IsNullOrWhiteSpace(imid) ? null : imid);
        return new OkObjectResult(await handler.HandleAsync(query, request.HttpContext.RequestAborted));
    }
}
