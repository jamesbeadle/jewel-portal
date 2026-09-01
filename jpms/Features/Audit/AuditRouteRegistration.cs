using Jewel.JPMS.Contracts.Audit;

namespace Jewel.JPMS.Features.Audit;

// Client route for the audit register. Mirrors the api endpoint in AuditEndpoints.
public static class AuditRouteRegistration
{
    public static void RegisterAuditRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListAuditEvents, AuditEventsPage>(
            new QueryRoute("/api/audit/events",
                query =>
                {
                    var q = (ListAuditEvents)query;
                    var url = $"/api/audit/events?take={q.Take}";
                    if (!string.IsNullOrWhiteSpace(q.ProjectId)) url += $"&projectId={Uri.EscapeDataString(q.ProjectId)}";
                    if (!string.IsNullOrWhiteSpace(q.Pathway)) url += $"&pathway={Uri.EscapeDataString(q.Pathway)}";
                    if (q.EventType is { } eventType) url += $"&eventType={eventType}";
                    if (!string.IsNullOrWhiteSpace(q.ActorEmail)) url += $"&actor={Uri.EscapeDataString(q.ActorEmail)}";
                    if (!string.IsNullOrWhiteSpace(q.Cursor)) url += $"&cursor={Uri.EscapeDataString(q.Cursor)}";
                    if (!string.IsNullOrWhiteSpace(q.RecordId)) url += $"&recordId={Uri.EscapeDataString(q.RecordId)}";
                    if (q.RecordType is { } recordType) url += $"&recordType={recordType}";
                    return url;
                }));
    }
}
