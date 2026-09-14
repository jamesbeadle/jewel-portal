using Jewel.JPMS.Contracts.Audit;

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

        AuditEventType? eventType = null;
        if (Opt("eventType") is { } raw && Enum.TryParse<AuditEventType>(raw, ignoreCase: true, out var parsed))
            eventType = parsed;

        // Who may read depends on the read's shape — one rule shared with the connector's
        // list_audit_trail (AuditReadGate).
        if (!AuditReadGate.Allows(signedInUser, recordId, eventType))
            return new StatusCodeResult(403);
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
