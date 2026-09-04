using Jewel.JPMS.Api.Features.DocumentControl;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The mailbox, readable (2026-08-31) — the parity audit's largest single read hole (docs/ai/11
/// §3). The Control Centre's triage actions were mirrored while nothing over the connector could
/// LIST the queue or READ an email that isn't already linked to a record. Each tool wraps the same
/// query handler its endpoint composes and carries the endpoint's own role gate: the queue and
/// search follow TriageRoles, the document-triage queue follows DocumentControlRoles, and the
/// project communications roll-up is every internal role, exactly as over HTTP.
/// </summary>
internal static class AiMailboxTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    /// <summary>A full email body can be hundreds of KB of HTML — clip and say so, the same
    /// bargain as read_source. Enough to triage; read_email_attachment covers the files.</summary>
    private const int MaxBodyChars = 30_000;

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    private static object Row(MailboxMessage message) => new
    {
        messageId = message.Id,
        internetMessageId = message.InternetMessageId,
        from = new { email = message.FromEmail, name = message.FromName },
        message.Subject,
        preview = message.BodyPreview,
        message.ReceivedAt,
        message.HasAttachments,
        tags = message.Categories,
        bucket = message.Bucket,
        conversationId = message.ConversationId,
        threadTags = message.ThreadTags
    };

    private static object Page(MailboxPage page) => new
    {
        ok = true,
        total = page.Total,
        nextCursor = page.NextCursor,
        matchedBySubject = page.MatchedBySubject,
        messages = page.Items.Select(Row)
    };

    public static IReadOnlyList<AiTool> Build()
    {
        return new List<AiTool>
        {
            new(
                "list_triage_queue",
                "The Control Centre's mailbox views, read live: view \"queue\" is untriaged Inbox "
                + "mail waiting for a decision (oldest first by default — the backlog clears from "
                + "page one), \"discarded\" is mail set aside, \"tagged\" is mail already carrying "
                + "JPMS tags (optionally filtered to specific tags — a record's stem like "
                + "JPMS/REQ-0012, or a communication family's tags). Paged by cursor; total is the "
                + "whole pile.",
                AiToolSchema.Object(
                    ("view", "string", "queue (default), discarded, or tagged.", false),
                    ("tags", "array", "Tagged view only: exact JPMS tag strings to filter to.", false),
                    ("cursor", "string", "The nextCursor from the previous page.", false),
                    ("take", "number", "Messages per page, default 25.", false),
                    ("newestFirst", "boolean", "true reads newest first. Default false.", false)),
                AiToolKind.Read,
                TriageRoles.AllowedToTriage,
                async (context, input, ct) =>
                {
                    var view = AiToolSchema.Text(input, "view")?.Trim().ToLowerInvariant() ?? "queue";
                    var cursor = AiToolSchema.Text(input, "cursor");
                    var take = Math.Clamp(AiToolSchema.Number(input, "take") ?? 25, 1, 100);
                    var newestFirst = AiToolSchema.Flag(input, "newestFirst") ?? false;

                    if (view == "queue")
                    {
                        var page = await context.Services
                            .GetRequiredService<IQueryHandler<ListInboxMessages, MailboxPage>>()
                            .HandleAsync(new ListInboxMessages(cursor, take, newestFirst), ct);
                        return Serialise(Page(page));
                    }
                    if (view == "discarded")
                    {
                        var page = await context.Services
                            .GetRequiredService<IQueryHandler<ListDiscardedMessages, MailboxPage>>()
                            .HandleAsync(new ListDiscardedMessages(cursor, take, newestFirst), ct);
                        return Serialise(Page(page));
                    }
                    if (view == "tagged")
                    {
                        var tags = Texts(input, "tags");
                        var page = await context.Services
                            .GetRequiredService<IQueryHandler<ListTaggedMessages, MailboxPage>>()
                            .HandleAsync(new ListTaggedMessages(cursor, take, tags, newestFirst), ct);
                        return Serialise(Page(page));
                    }
                    return Fail("view must be queue, discarded or tagged.");
                }),

            new(
                "get_mailbox_message",
                "One mailbox email in full, read live: sanitised body, envelope (from/to/cc, "
                + "subject), its current JPMS tags and pathway bucket, its attachments with the "
                + "ids read_email_attachment takes, and the replyAll envelope a reply starts from "
                + "(what send_mailbox_email takes). Works for ANY mailbox message — triaged or "
                + "not. A very long body is clipped and the result says so.",
                AiToolSchema.Object(
                    ("messageId", "string", "The message id from list_triage_queue, search_mailbox or a communications listing.", true),
                    ("internetMessageId", "string", "The stable fallback id from the same listing — pass it when you have it.", false)),
                AiToolKind.Read,
                TriageRoles.AllowedToTriage,
                async (context, input, ct) =>
                {
                    var messageId = AiToolSchema.Text(input, "messageId")?.Trim();
                    if (string.IsNullOrWhiteSpace(messageId))
                        return Fail("A messageId is required — list_triage_queue and search_mailbox return them.");

                    var internetMessageId = AiToolSchema.Text(input, "internetMessageId");
                    var detail = await context.Services
                        .GetRequiredService<IQueryHandler<GetMailboxMessageDetail, MailboxMessageDetail>>()
                        .HandleAsync(new GetMailboxMessageDetail(messageId, internetMessageId), ct);

                    var body = detail.BodyHtml ?? "";
                    var clipped = body.Length > MaxBodyChars;
                    var replyAll = ReplyAllEnvelope.For(detail);

                    return Serialise(new
                    {
                        ok = true,
                        messageId = detail.MessageId,
                        from = new { email = detail.FromEmail, name = detail.FromName },
                        to = detail.To,
                        cc = detail.Cc,
                        replyTo = detail.ReplyTo,
                        mailboxAddress = detail.MailboxAddress,
                        subject = detail.Subject,
                        tags = detail.Categories,
                        bucket = detail.Bucket,
                        body = clipped ? body[..MaxBodyChars] : body,
                        bodyClipped = clipped,
                        attachments = detail.Attachments,
                        replyAll = new { to = replyAll.To, cc = replyAll.Cc, subject = replyAll.Subject },
                        note = "Attachments open with read_email_attachment(messageId, attachmentId). "
                            + "To answer this email, send_mailbox_email (perform_action) takes "
                            + "replyToMessageId = messageId (replyToInternetMessageId = the listing "
                            + "row's internetMessageId, when you have it) plus replyAll's to/cc/subject."
                    });
                }),

            new(
                "list_mailbox_conversation",
                "An email's whole thread: every Inbox message sharing its conversation, oldest "
                + "first, whatever their tags. Later replies often say how the earlier messages "
                + "should be triaged — read the thread before deciding. Pass the subject too, so a "
                + "conversation whose id has split from the chain still finds its members.",
                AiToolSchema.Object(
                    ("conversationId", "string", "The conversationId from a message listing.", true),
                    ("subject", "string", "The email's subject — the by-subject fallback when the id has split.", false)),
                AiToolKind.Read,
                TriageRoles.AllowedToTriage,
                async (context, input, ct) =>
                {
                    var conversationId = AiToolSchema.Text(input, "conversationId")?.Trim();
                    if (string.IsNullOrWhiteSpace(conversationId))
                        return Fail("A conversationId is required — message listings return it.");

                    var page = await context.Services
                        .GetRequiredService<IQueryHandler<ListConversationMessages, MailboxPage>>()
                        .HandleAsync(new ListConversationMessages(conversationId, AiToolSchema.Text(input, "subject")), ct);
                    return Serialise(Page(page));
                }),

            new(
                "search_mailbox",
                "Search the projects mailbox — sender, subject and body text — returning matching "
                + "messages newest first with their tags. The same search the Control Centre's "
                + "email finder runs.",
                AiToolSchema.Object(
                    ("query", "string", "What to search for.", true),
                    ("take", "number", "Maximum results, default 25.", false)),
                AiToolKind.Read,
                TriageRoles.AllowedToTriage,
                async (context, input, ct) =>
                {
                    var query = AiToolSchema.Text(input, "query")?.Trim();
                    if (string.IsNullOrWhiteSpace(query)) return Fail("A query is required.");

                    var messages = await context.Services
                        .GetRequiredService<IQueryHandler<SearchMailboxMessages, IReadOnlyList<MailboxMessage>>>()
                        .HandleAsync(new SearchMailboxMessages(query, Math.Clamp(AiToolSchema.Number(input, "take") ?? 25, 1, 100)), ct);
                    return Serialise(new { ok = true, count = messages.Count, messages = messages.Select(Row) });
                }),

            new(
                "list_document_triage",
                "The Document Triage queue — email attachments sent over from the Control Centre, "
                + "each waiting to be filed to a project's Documents register, Payment Certificates or a subcontractor's "
                + "compliance documents, or already filed/discarded (the filed rows keep their "
                + "where-it-went history). Filter by status: Pending (default), Filed, Discarded, "
                + "or all.",
                AiToolSchema.Object(
                    ("status", "string", "Pending (default), Filed, Discarded, or all.", false)),
                AiToolKind.Read,
                DocumentControlRoles.AllowedToManage,
                async (context, input, ct) =>
                {
                    var items = await context.Services
                        .GetRequiredService<IQueryHandler<ListDocumentControlItems, IReadOnlyList<DocumentControlItem>>>()
                        .HandleAsync(new ListDocumentControlItems(), ct);

                    var statusText = AiToolSchema.Text(input, "status")?.Trim() ?? "Pending";
                    if (!string.Equals(statusText, "all", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!Enum.TryParse<DocumentControlStatus>(statusText, ignoreCase: true, out var status))
                            return Fail("status must be Pending, Filed, Discarded or all.");
                        items = items.Where(item => item.Status == status).ToList();
                    }

                    return Serialise(new
                    {
                        ok = true,
                        count = items.Count,
                        items = items.Select(item => new
                        {
                            item.DocumentControlItemId,
                            item.FileName,
                            item.ContentType,
                            item.FileSizeBytes,
                            from = new { email = item.FromEmail, name = item.FromName },
                            item.Subject,
                            item.ReceivedAt,
                            item.ProjectIdHint,
                            status = item.Status.ToString(),
                            item.SentBy,
                            item.SentAt,
                            item.ResolvedBy,
                            item.ResolvedAt,
                            filedAs = item.FiledAs?.ToString(),
                            item.FiledLabel,
                            sourceMessageId = item.MessageId,
                            sourceAttachmentId = item.AttachmentId
                        })
                    });
                }),

            new(
                "list_project_communications",
                "A project's Communications tab, read live: every mailbox email tagged to any of "
                + "the project's records, newest first, each with the records it is filed under. "
                + "Narrow by record type or pathway bucket (Client, Subcontractor, Internal). "
                + "Paged by cursor; a bucket filter reports total 0, meaning count unknown. "
                + "Pass search to find emails within the project's tagged mail by subject, body, "
                + "sender or attachment name (relevance-ordered, one page, no cursor).",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false),
                    ("type", "string", "Only emails filed under this record type (e.g. Request, VariationQuote, WorkOrder).", false),
                    ("bucket", "string", "Only this pathway: Client, Subcontractor or Internal.", false),
                    ("search", "string", "Free text to find within the project's tagged emails.", false),
                    ("cursor", "string", "The nextCursor from the previous page.", false),
                    ("take", "number", "Messages per page, default 25.", false)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var projectId = AiToolSchema.Text(input, "projectId") ?? context.Scope?.ProjectId;
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids).");

                    RecordType? type = null;
                    var typeText = AiToolSchema.Text(input, "type");
                    if (!string.IsNullOrWhiteSpace(typeText))
                    {
                        if (!Enum.TryParse<RecordType>(typeText, ignoreCase: true, out var parsed))
                            return Fail($"\"{typeText}\" is not a record type.");
                        type = parsed;
                    }

                    var page = await context.Services
                        .GetRequiredService<IQueryHandler<ListProjectCommunications, ProjectCommunicationsPage>>()
                        .HandleAsync(new ListProjectCommunications(
                            projectId,
                            type,
                            AiToolSchema.Text(input, "cursor"),
                            Math.Clamp(AiToolSchema.Number(input, "take") ?? 25, 1, 100),
                            AiToolSchema.Text(input, "bucket"),
                            AiToolSchema.Text(input, "search")), ct);

                    return Serialise(new
                    {
                        ok = true,
                        total = page.Total,
                        nextCursor = page.NextCursor,
                        communications = page.Items.Select(item => new
                        {
                            message = Row(item.Message),
                            links = item.Links.Select(link => new
                            {
                                type = link.Type.ToString(),
                                link.Reference,
                                link.Title,
                                link.Tag
                            })
                        })
                    });
                })
        };
    }

    private static IReadOnlyList<string>? Texts(JsonElement input, string name)
    {
        if (input.ValueKind != JsonValueKind.Object
            || !input.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.Array) return null;
        var items = value.EnumerateArray()
            .Where(element => element.ValueKind == JsonValueKind.String)
            .Select(element => element.GetString()!)
            .ToList();
        return items.Count == 0 ? null : items;
    }
}
