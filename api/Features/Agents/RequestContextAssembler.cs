using System.Text;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Agents;

// Gathers everything an agent needs to "see" a request — the request header, its in-app/email
// conversation (RequestMessages) and the originating intake emails — into one text context. The stub
// agents ignore the result, but it is built so a real agent can hand it straight to Claude.
//
// Today attachments are metadata-only (bodies are fetched live from Graph elsewhere); when the real
// agents arrive this assembler is the single place to extend to fetch attachment bytes.
public sealed class RequestContextAssembler
{
    private readonly JpmsContext context;

    public RequestContextAssembler(JpmsContext context) { this.context = context; }

    public async Task<RequestAgentContext?> AssembleAsync(string requestId, CancellationToken cancellationToken)
    {
        var request = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == requestId, cancellationToken);
        if (request is null) return null;

        var header = BuildHeader(request);
        var conversation = await BuildConversationAsync(requestId, cancellationToken);

        // In the live-read model the originating email is captured into the conversation (RequestMessages)
        // when it is assigned/created, so there is no separate intake-email section to assemble.
        return new RequestAgentContext(requestId, header, conversation, "");
    }

    private static string BuildHeader(Data.Entities.RequestEntity r)
    {
        var sb = new StringBuilder();
        var number = r.Number > 0 ? $"REQ-{r.Number:0000}" : "(unnumbered)";
        sb.AppendLine($"Number: {number}");
        sb.AppendLine($"Project: {r.ProjectId}");
        sb.AppendLine($"Type: {((RequestType)r.Kind).LongName()}");
        sb.AppendLine($"Reference: {r.Reference}");
        sb.AppendLine($"Title: {r.Title}");
        sb.AppendLine($"Status: {(RequestStatus)r.Status}");
        if (r.Value is not null) sb.AppendLine($"Value: {r.Value:N2}");
        if (!string.IsNullOrWhiteSpace(r.RaisedTo)) sb.AppendLine($"Ball-in-court: {r.RaisedTo}");
        if (!string.IsNullOrWhiteSpace(r.DrawingRef)) sb.AppendLine($"Drawing ref: {r.DrawingRef}");
        if (r.ResponseDue is not null) sb.AppendLine($"Response due: {r.ResponseDue:yyyy-MM-dd}");
        sb.AppendLine($"Raised by: {r.RaisedByEmail} on {r.RaisedAt:yyyy-MM-dd}");
        sb.AppendLine("Description:");
        sb.AppendLine(r.Description);
        if (!string.IsNullOrWhiteSpace(r.ResponseText))
        {
            sb.AppendLine("Response:");
            sb.AppendLine(r.ResponseText);
        }
        return sb.ToString();
    }

    private async Task<string> BuildConversationAsync(string requestId, CancellationToken cancellationToken)
    {
        var messages = await context.RequestMessages
            .Where(m => m.RequestId == requestId)
            .OrderBy(m => m.PostedAt)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return "";

        var sb = new StringBuilder();
        foreach (var m in messages)
        {
            var direction = (MessageDirection)m.Direction;
            sb.AppendLine($"[{m.PostedAt:yyyy-MM-dd HH:mm}] {m.AuthorName} <{m.AuthorEmail}> ({direction}):");
            sb.AppendLine(m.Body);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

}
