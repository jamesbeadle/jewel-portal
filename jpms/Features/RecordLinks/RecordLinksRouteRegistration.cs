
namespace Jewel.JPMS.Features.RecordLinks;

// Client routes for the record-agnostic link layer: list a project's records of a type (the
// category-first triage picker) and link a message to one. Mirrors the api endpoints in
// RecordLinksEndpoints. The record type goes in the query string; the message id travels in the body.
public static class RecordLinksRouteRegistration
{
    public static IServiceCollection AddRecordLinksReadModels(this IServiceCollection services)
    {
        services.AddScoped<RecordActivityReadModel>();
        return services;
    }

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

        queries.Register<GetProgrammeEmailDetail, MailboxMessageDetail>(
            new QueryRoute("/api/projects/{projectId}/programme/emails/detail",
                query =>
                {
                    var q = (GetProgrammeEmailDetail)query;
                    var url = $"/api/projects/{q.ProjectId}/programme/emails/detail?id={Uri.EscapeDataString(q.MessageId)}";
                    if (!string.IsNullOrWhiteSpace(q.InternetMessageId)) url += $"&imid={Uri.EscapeDataString(q.InternetMessageId)}";
                    return url;
                }));

        queries.Register<ListRecordEmails, IReadOnlyList<MailboxMessage>>(
            new QueryRoute("/api/records/{type}/{recordId}/emails",
                query =>
                {
                    var q = (ListRecordEmails)query;
                    return $"/api/records/{q.Type}/{Uri.EscapeDataString(q.RecordId)}/emails";
                }));

        // The replies a record page is blind to: newer thread members not yet tagged to it.
        queries.Register<ListUnfiledReplies, IReadOnlyList<MailboxMessage>>(
            new QueryRoute("/api/records/{type}/{recordId}/unfiled-replies",
                query =>
                {
                    var q = (ListUnfiledReplies)query;
                    return $"/api/records/{q.Type}/{Uri.EscapeDataString(q.RecordId)}/unfiled-replies";
                }));

        // Free-text mailbox search behind the record pages' "Find emails" dialog.
        queries.Register<SearchMailboxMessages, IReadOnlyList<MailboxMessage>>(
            new QueryRoute("/api/mailbox/search",
                query =>
                {
                    var q = (SearchMailboxMessages)query;
                    return $"/api/mailbox/search?q={Uri.EscapeDataString(q.Query)}&take={q.Take}";
                }));

        // Mailbox tag stems back to the records they name — the tagged-email search's record
        // chips. Stems never contain commas, so a comma-joined list travels safely in one param.
        queries.Register<ResolveRecordTags, IReadOnlyList<LinkableRecord>>(
            new QueryRoute("/api/mailbox/tags/resolve",
                query =>
                {
                    var q = (ResolveRecordTags)query;
                    return $"/api/mailbox/tags/resolve?tags={Uri.EscapeDataString(string.Join(',', q.Tags))}";
                }));

        // One call per project page view feeds every activity badge on it (register rows, record
        // tab dots) — derived server-side from the audit trail's link events, never the mailbox.
        queries.Register<ListRecordActivity, IReadOnlyList<RecordActivitySummary>>(
            new QueryRoute("/api/projects/{projectId}/records/activity",
                query => $"/api/projects/{((ListRecordActivity)query).ProjectId}/records/activity"));

        queries.Register<ListProjectCommunications, ProjectCommunicationsPage>(
            new QueryRoute("/api/projects/{projectId}/communications",
                query =>
                {
                    var q = (ListProjectCommunications)query;
                    var url = $"/api/projects/{q.ProjectId}/communications?take={q.Take}";
                    if (q.Type is { } type) url += $"&type={type}";
                    if (!string.IsNullOrWhiteSpace(q.Bucket)) url += $"&bucket={Uri.EscapeDataString(q.Bucket)}";
                    if (!string.IsNullOrWhiteSpace(q.Cursor)) url += $"&cursor={Uri.EscapeDataString(q.Cursor)}";
                    if (!string.IsNullOrWhiteSpace(q.Search)) url += $"&q={Uri.EscapeDataString(q.Search)}";
                    return url;
                }));

        commands.Register<LinkMessageToRecord, Acknowledgement>(
            new CommandRoute("POST", "/api/mailbox/message/link", _ => "/api/mailbox/message/link"));

        commands.Register<PrepareProgrammeReplyDraft, ProgrammeReplyDraft>(
            new CommandRoute("POST", "/api/projects/{projectId}/programme/emails/reply-draft",
                command => $"/api/projects/{((PrepareProgrammeReplyDraft)command).ProjectId}/programme/emails/reply-draft"));
    }
}
