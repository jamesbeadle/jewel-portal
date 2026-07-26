using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Audit;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Audit;

// The audit register read. Internal oversight surface — gated like the rest of triage (the people
// who make the decisions are the people who review them); never client-visible.
public sealed class AuditEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListAuditEvents, AuditEventsPage> list;

    public AuditEndpoints(SignedInUserResolver users, IQueryHandler<ListAuditEvents, AuditEventsPage> list)
    {
        this.users = users;
        this.list = list;
    }

    [Function(nameof(ListAuditEvents))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "audit/events")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        string? Opt(string name) { var v = request.Query[name].ToString(); return string.IsNullOrWhiteSpace(v) ? null : v; }
        var recordId = Opt("recordId");

        // The whole register is an oversight surface, gated like triage. A read narrowed to ONE
        // record is that record's own history — the History panel on the request page — so it opens
        // to the internal team: the people who can draft the correspondence must be able to see that
        // it was drafted. Anything unnarrowed still needs the triage gate.
        var gate = string.IsNullOrWhiteSpace(recordId)
            ? TriageRoles.AllowedToTriage
            : JpmsRoleSets.AllInternal;
        if (!signedInUser.Roles.Contains(Role.Admin) && !gate.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(403);

        AuditEventType? eventType = null;
        if (Opt("eventType") is { } raw && Enum.TryParse<AuditEventType>(raw, ignoreCase: true, out var parsed))
            eventType = parsed;
        RecordType? recordType = null;
        if (Opt("recordType") is { } rawRecordType && Enum.TryParse<RecordType>(rawRecordType, ignoreCase: true, out var parsedRecordType))
            recordType = parsedRecordType;
        var take = int.TryParse(Opt("take"), out var t) ? t : 50;

        var query = new ListAuditEvents(
            Opt("projectId"), Opt("pathway"), eventType, Opt("actor"), Opt("cursor"), take,
            recordId, recordType);
        return new OkObjectResult(await list.HandleAsync(query, request.HttpContext.RequestAborted));
    }
}
