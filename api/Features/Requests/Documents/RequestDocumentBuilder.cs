using Jewel.JPMS.Api.Data;
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
        JpmsContext context, string requestId, CancellationToken ct)
    {
        var request = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == requestId, ct);
        if (request is null)
            return null;

        var project = await context.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, ct);

        // Recipients are the project's contacts flagged to receive request documents, in a stable
        // order (role, then name) so the issued-to list reads the same on every regeneration.
        var recipients = await context.ProjectContacts
            .Where(c => c.ProjectId == request.ProjectId && c.ReceivesRequests)
            .OrderBy(c => c.Role)
            .ThenBy(c => c.Name)
            .Select(c => new RequestDocumentRecipient(
                c.Name, c.Email, ((ProjectContactRole)c.Role).DisplayName(), c.Organisation))
            .ToListAsync(ct);

        // Only the Shared leg of the thread belongs on a client-facing document; internal Jewel
        // discussion never leaves the platform.
        var activity = await context.RequestMessages
            .Where(m => m.RequestId == requestId && m.Visibility == (int)MessageVisibility.Shared)
            .OrderBy(m => m.PostedAt)
            .Select(m => new RequestDocumentActivity(
                m.AuthorName, m.Body, m.PostedAt, m.Direction == (int)MessageDirection.Inbound))
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
            GeneratedAt: DateTimeOffset.UtcNow);
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
