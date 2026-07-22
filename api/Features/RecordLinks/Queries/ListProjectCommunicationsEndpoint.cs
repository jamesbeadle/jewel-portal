using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// GET a project's tagged emails for its Communications tab, optionally narrowed to one record type
// (?type=CostCentre). Like ListSchedulingEmails, reading a record's mail is a project-view concern,
// not a triage one — but it is still internal-only mailbox content, so the gate is every internal
// role (never external portal logins).
public sealed class ListProjectCommunicationsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListProjectCommunications, ProjectCommunicationsPage> handler;

    public ListProjectCommunicationsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListProjectCommunications, ProjectCommunicationsPage> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Internal mailbox content: every internal role, no externals.
    private static readonly RoleSet RolesThatMayReadCommunications = JpmsRoleSets.AllInternal;

    [Function(nameof(ListProjectCommunications))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/communications")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadCommunications.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        // Type is optional (absent = every linkable type) but must parse when present — a typo'd
        // type should fail loudly rather than silently widening to everything.
        RecordType? type = null;
        var typeRaw = request.Query["type"].ToString();
        if (!string.IsNullOrWhiteSpace(typeRaw))
        {
            if (!Enum.TryParse<RecordType>(typeRaw, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                return new BadRequestObjectResult("A valid record type is required when type is given (e.g. type=CostCentre).");
            type = parsed;
        }

        var cursor = request.Query["cursor"].ToString();
        var take = int.TryParse(request.Query["take"].ToString(), out var t) ? t : 25;

        // Bucket is optional (absent = every pathway) but must be one of the three when present.
        var bucketRaw = request.Query["bucket"].ToString();
        string? bucket = null;
        if (!string.IsNullOrWhiteSpace(bucketRaw))
        {
            if (!bucketRaw.Equals("Client", StringComparison.OrdinalIgnoreCase)
                && !bucketRaw.Equals("Subcontractor", StringComparison.OrdinalIgnoreCase)
                && !bucketRaw.Equals("Internal", StringComparison.OrdinalIgnoreCase))
                return new BadRequestObjectResult("bucket must be Client, Subcontractor or Internal when given.");
            bucket = char.ToUpperInvariant(bucketRaw[0]) + bucketRaw[1..].ToLowerInvariant();
        }

        var result = await handler.HandleAsync(
            new ListProjectCommunications(projectId, type, string.IsNullOrWhiteSpace(cursor) ? null : cursor, take, bucket),
            request.HttpContext.RequestAborted);
        return new OkObjectResult(result);
    }
}
