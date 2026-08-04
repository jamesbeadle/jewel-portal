using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// GET a project's per-record activity summaries — one call feeds every activity badge on a page
// (register rows, record tab dots). Activity is metadata about internal mailbox correspondence,
// so the gate matches ListRecordEmails: every internal role, never external portal logins.
public sealed class ListRecordActivityEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListRecordActivity, IReadOnlyList<RecordActivitySummary>> handler;

    public ListRecordActivityEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListRecordActivity, IReadOnlyList<RecordActivitySummary>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Internal mailbox metadata: every internal role, no externals.
    private static readonly RoleSet RolesThatMayReadActivity = JpmsRoleSets.AllInternal;

    [Function(nameof(ListRecordActivity))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/records/activity")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadActivity.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        return new OkObjectResult(
            await handler.HandleAsync(new ListRecordActivity(projectId), request.HttpContext.RequestAborted));
    }
}
