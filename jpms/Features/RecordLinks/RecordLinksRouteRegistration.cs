using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.RecordLinks;

// Client routes for the record-agnostic link layer: list a project's records of a type (the
// category-first triage picker) and link a message to one. Mirrors the api endpoints in
// RecordLinksEndpoints. The record type goes in the query string; the message id travels in the body.
public static class RecordLinksRouteRegistration
{
    public static void RegisterRecordLinksRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListLinkableRecords, IReadOnlyList<LinkableRecord>>(
            new QueryRoute("/api/projects/{projectId}/records",
                query =>
                {
                    var q = (ListLinkableRecords)query;
                    return $"/api/projects/{q.ProjectId}/records?type={q.Type}";
                }));

        queries.Register<ListSchedulingEmails, IReadOnlyList<MailboxMessage>>(
            new QueryRoute("/api/projects/{projectId}/scheduling/emails",
                query => $"/api/projects/{((ListSchedulingEmails)query).ProjectId}/scheduling/emails"));

        queries.Register<ListRecordEmails, IReadOnlyList<MailboxMessage>>(
            new QueryRoute("/api/records/{type}/{recordId}/emails",
                query =>
                {
                    var q = (ListRecordEmails)query;
                    return $"/api/records/{q.Type}/{Uri.EscapeDataString(q.RecordId)}/emails";
                }));

        queries.Register<ListProjectCommunications, ProjectCommunicationsPage>(
            new QueryRoute("/api/projects/{projectId}/communications",
                query =>
                {
                    var q = (ListProjectCommunications)query;
                    var url = $"/api/projects/{q.ProjectId}/communications?take={q.Take}";
                    if (q.Type is { } type) url += $"&type={type}";
                    if (!string.IsNullOrWhiteSpace(q.Bucket)) url += $"&bucket={Uri.EscapeDataString(q.Bucket)}";
                    if (!string.IsNullOrWhiteSpace(q.Cursor)) url += $"&cursor={Uri.EscapeDataString(q.Cursor)}";
                    return url;
                }));

        commands.Register<LinkMessageToRecord, Acknowledgement>(
            new CommandRoute("POST", "/api/mailbox/message/link", _ => "/api/mailbox/message/link"));

        commands.Register<SyncRecordThreadTags, Acknowledgement>(
            new CommandRoute("POST", "/api/mailbox/record/sync-thread-tags", _ => "/api/mailbox/record/sync-thread-tags"));
    }
}
