using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Audit;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The System Audit Trail over the connector (2026-09-14, the MD's rule: the connector mirrors the
/// site, so what /audit shows the assistant can read). One tool wrapping the SAME query handler the
/// page composes (ListAuditEvents) behind the SAME shape-dependent gate the endpoint applies
/// (AuditReadGate): the whole register for the triage roles, one record's own history for the
/// internal team, the cost-centre recode trail for the commercial team, the KPI events for
/// administrators only. The catalogue shows it to everyone internal — the widest a read can open to
/// — and the call re-checks the gate for the read actually asked for.
/// </summary>
internal static class AiAuditTools
{
    public const string ListAuditTrail = "list_audit_trail";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };
    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public static IReadOnlyList<AiTool> Build() => new AiTool[]
    {
        new(
            ListAuditTrail,
            "The System Audit Trail (/audit), newest first: who did what, when — every triage "
            + "decision (email routed, record linked, tag removed, discarded, thread swept), "
            + "record created from email, draft or email sent, snapshot frozen, work order "
            + "approved/rejected/cancelled/accepted from the PO email's link, cost-centre recode, budget set, labour correction, "
            + "Xero link and the rest. Each row carries the actor's email, the event, the "
            + "pathway, the project, the record (type, id, reference), the email or conversation "
            + "it concerns and one plain sentence of detail. Filters compose: projectId, pathway "
            + "(Client / Subcontractor / Internal), eventType (an AuditEventType name — "
            + "RecordLinked, EmailTriaged, TagRemoved, ThreadSwept, WorkOrderApproved, "
            + "CostCentreRecoded…), actorEmail, and recordId + recordType for ONE record's own "
            + "history (\"who tagged this thread to WO-0045\": recordType work_order with the "
            + "workOrderId from find_by_reference). Paged by cursor; total is the whole register "
            + "for those filters. Reading the register whole needs the triage roles; one record's "
            + "history opens to the internal team.",
            AiToolSchema.Object(
                ("projectId", "string", "Only this project's events (list_projects returns ids).", false),
                ("pathway", "string", "Client, Subcontractor or Internal.", false),
                ("eventType", "string", "One AuditEventType name, e.g. RecordLinked.", false),
                ("actorEmail", "string", "Only events by this person (their sign-in email).", false),
                ("recordId", "string", "One record's own history — its id (with recordType).", false),
                ("recordType", "string", "The record's type: request, work_order, bid_package, variation_quote, todo, defect…", false),
                ("cursor", "string", "The nextCursor from the previous page.", false),
                ("take", "number", "Rows per page, default 50, at most 200.", false)),
            AiToolKind.Read,
            JpmsRoleSets.AllInternal,
            ListAuditTrailAsync)
    };

    private static async Task<string> ListAuditTrailAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var recordId = AiToolSchema.Text(input, "recordId")?.Trim();
        var eventTypeName = AiToolSchema.Text(input, "eventType")?.Trim();
        var recordTypeName = AiToolSchema.Text(input, "recordType")?.Trim();

        AuditEventType? eventType = null;
        if (!string.IsNullOrWhiteSpace(eventTypeName))
        {
            if (!Enum.TryParse<AuditEventType>(eventTypeName, ignoreCase: true, out var parsedEventType))
                return Fail($"Unknown eventType '{eventTypeName}' — use an AuditEventType name such as RecordLinked or EmailTriaged.");
            eventType = parsedEventType;
        }

        RecordType? recordType = null;
        if (!string.IsNullOrWhiteSpace(recordTypeName))
        {
            if (!AiRecordTools.TryMapRecordType(recordTypeName, out var parsedRecordType))
                return Fail($"Unknown recordType '{recordTypeName}' — say work_order, request, bid_package, variation, todo, defect…");
            recordType = parsedRecordType;
        }

        if (!AuditReadGate.Allows(context.User, recordId, eventType))
            return Fail("The audit trail is for the triage roles (administrators, directors, project managers, the finance director); one record's own history opens to the internal team.");

        var page = await context.Services
            .GetRequiredService<IQueryHandler<ListAuditEvents, AuditEventsPage>>()
            .HandleAsync(
                new ListAuditEvents(
                    AiToolSchema.Text(input, "projectId")?.Trim(),
                    AiToolSchema.Text(input, "pathway")?.Trim(),
                    eventType,
                    AiToolSchema.Text(input, "actorEmail")?.Trim(),
                    AiToolSchema.Text(input, "cursor")?.Trim(),
                    AiToolSchema.Number(input, "take") ?? 50,
                    recordId,
                    recordType),
                ct);

        return Serialise(new
        {
            ok = true,
            count = page.Items.Count,
            total = page.Total,
            nextCursor = page.NextCursor,
            events = page.Items.Select(Row),
            note = "Newest first. eventType names are the AuditEventType enum. A row's messageId "
                + "opens with get_mailbox_message; conversationId with list_mailbox_conversation; "
                + "webLink is the email or draft in Outlook on the web. Route: /audit."
        });
    }

    private static object Row(AuditEvent item) => new
    {
        auditEventId = item.AuditEventId,
        occurredAt = item.OccurredAt,
        actor = item.ActorEmail,
        eventType = item.EventType.ToString(),
        pathway = string.IsNullOrEmpty(item.Pathway) ? null : item.Pathway,
        projectId = item.ProjectId,
        recordType = item.RecordType?.ToString(),
        recordId = item.RecordId,
        recordReference = string.IsNullOrEmpty(item.RecordReference) ? null : item.RecordReference,
        detail = item.Detail,
        conversationId = item.ConversationId,
        messageId = item.EmailMessageId,
        internetMessageId = item.InternetMessageId,
        webLink = item.WebLink
    };
}
