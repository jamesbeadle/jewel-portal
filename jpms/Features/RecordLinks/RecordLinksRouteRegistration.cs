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

        commands.Register<LinkMessageToRecord, Acknowledgement>(
            new CommandRoute("POST", "/api/mailbox/message/link", _ => "/api/mailbox/message/link"));
    }
}
