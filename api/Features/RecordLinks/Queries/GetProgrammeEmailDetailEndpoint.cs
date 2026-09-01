using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// GET the full body of one programme-tagged email, for the Programme tab's Communications view.
// Same gate as listing the emails (every internal role, never external portal logins); the handler
// enforces that the message actually carries the programme tag before returning any content. The
// message id travels in the query string, not the path (Graph ids contain path-unsafe characters).
public sealed class GetProgrammeEmailDetailEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetProgrammeEmailDetail, MailboxMessageDetail> handler;

    public GetProgrammeEmailDetailEndpoint(SignedInUserResolver users, IQueryHandler<GetProgrammeEmailDetail, MailboxMessageDetail> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Internal mailbox content: every internal role, no externals — mirrors ListSchedulingEmails.
    private static readonly RoleSet RolesThatMayReadProgrammeEmails = JpmsRoleSets.AllInternal;

    [Function(nameof(GetProgrammeEmailDetail))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/programme/emails/detail")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadProgrammeEmails.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var id = request.Query["id"].ToString();
        if (string.IsNullOrWhiteSpace(id)) return new BadRequestObjectResult("id is required.");
        var imid = request.Query["imid"].ToString();
        var query = new GetProgrammeEmailDetail(projectId, id, string.IsNullOrWhiteSpace(imid) ? null : imid);
        return new OkObjectResult(await handler.HandleAsync(query, request.HttpContext.RequestAborted));
    }
}
