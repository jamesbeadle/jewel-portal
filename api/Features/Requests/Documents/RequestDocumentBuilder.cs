using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Requests.Recipients;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Documents;

/// <summary>
/// Collates a <see cref="RequestDocumentModel"/> from the SQL source of truth. Pure read: it never
/// mutates state, so calling it on creation, on download, or on every resend always reflects the
/// request exactly as it currently stands (idempotent regeneration — nothing is persisted).
/// </summary>
public static class RequestDocumentBuilder
{
    public static async Task<RequestDocumentModel?> BuildAsync(
        JpmsContext context, string requestId, IReadOnlyList<MailboxMessage> emails, CancellationToken ct,
        RequestRecipientSet? resolvedRecipients = null)
    {
        var request = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == requestId, ct);
        if (request is null)
            return null;

        var project = await context.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, ct);

        // Recipients come from the shared resolver (request party → project party → project
        // profile), so the document's issued-to block always matches what an actual send would
        // address. Only To and Cc reach the document — Bcc stays off every client-facing surface.
        // Callers that already resolved (the worker send, the draft) pass their set in so a single
        // send never resolves twice.
        var recipientSet = resolvedRecipients
            ?? await RequestRecipientResolver.ResolveAsync(context, request, ct);
        var recipients = recipientSet.To.Concat(recipientSet.Cc)
            .Select(r => new RequestDocumentRecipient(
                r.Name, r.Email, r.RoleLabel ?? "Contact", r.Organisation, r.Routing))
            .ToList();

        // Only the Shared leg of the thread belongs on a client-facing document; internal Jewel
        // discussion never leaves the platform. In-app Shared activity comes from SQL; the inbound
        // email leg is the emails tagged to this request, read live (legacy stored Inbound rows are
        // excluded so they don't double up). The two are merged and ordered by time.
        var storedActivity = await context.RequestMessages
            .Where(m => m.RequestId == requestId
                && m.Visibility == (int)MessageVisibility.Shared
                && m.Direction != (int)MessageDirection.Inbound)
            .Select(m => new RequestDocumentActivity(m.AuthorName, m.Body, m.PostedAt, false))
            .ToListAsync(ct);

        var activity = storedActivity
            .Concat(emails.Select(e => new RequestDocumentActivity(
                string.IsNullOrWhiteSpace(e.FromName) ? e.FromEmail : e.FromName,
                string.IsNullOrWhiteSpace(e.BodyPreview) ? "(no message body)" : e.BodyPreview,
                e.ReceivedAt, true)))
            .OrderBy(a => a.PostedAt)
            .ToList();

        // The official document's itemised queries, in display order.
        var items = await context.RequestItems
            .Where(i => i.RequestId == requestId)
            .OrderBy(i => i.Position)
            .Select(i => new RequestDocumentItem(i.Position, i.DrawingRef, i.MemberArea, i.Query, i.Response))
            .ToListAsync(ct);

        var kind = (RequestType)request.Kind;
        var status = (RequestStatus)request.Status;

        return new RequestDocumentModel(
            RequestId: request.RequestId,
            DisplayNumber: request.Number > 0 ? $"REQ-{request.Number:0000}" : "",
            TypeShort: kind.DisplayName(),
            TypeLong: kind.LongName(),
            Title: request.Title,
            Description: request.Description,
            StatusLabel: StatusLabel(status),
            ProjectName: project?.Name ?? "(unknown project)",
            ProjectReference: project?.Reference ?? request.ProjectId,
            ClientName: project?.ClientName ?? "",
            RaisedByEmail: request.RaisedByEmail,
            RaisedAt: request.RaisedAt,
            ResponseDue: request.ResponseDue,
            RaisedTo: request.RaisedTo,
            DrawingRef: request.DrawingRef,
            RelatedDrawingSpec: request.RelatedDrawingSpec,
            Value: request.Value,
            ImpliesVariation: request.ImpliesVariation,
            ClientNotes: request.ClientNotes,
            ResponseText: request.ResponseText,
            RespondedByEmail: request.RespondedByEmail,
            RespondedAt: request.RespondedAt,
            Recipients: recipients,
            Activity: activity,
            GeneratedAt: DateTimeOffset.UtcNow,
            Reference: request.Reference,
            BasisOfQueries: request.BasisOfQueries,
            ResponseActionRequired: request.ResponseActionRequired,
            ImpactIfLate: request.ImpactIfLate,
            Items: items);
    }

    private static string StatusLabel(RequestStatus status) => status switch
    {
        RequestStatus.Open             => "Open",
        RequestStatus.AwaitingResponse => "Awaiting response",
        RequestStatus.Approved         => "Approved",
        RequestStatus.Rejected         => "Rejected",
        RequestStatus.Closed           => "Closed",
        RequestStatus.Responded        => "Responded",
        _ => status.ToString()
    };
}
