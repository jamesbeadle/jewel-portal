using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Gates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

/// <summary>
/// GET /api/requests/{requestId}/messages/email-attachment?id=…&amp;aid=…&amp;imid=…&amp;inline=1 —
/// streams one attachment of an email in a request's conversation, so readers of the record can
/// open the file without the triage roles the mailbox-wide download endpoint requires. Ids travel
/// in the query string, never the route path (Graph ids contain path-unsafe characters).
///
/// Same gate as reading the conversation, and the same membership check as the email-detail query:
/// the message must currently carry the request's tag (re-found by internetMessageId when the Graph
/// id has gone stale), so this endpoint cannot be used to read arbitrary mailbox messages.
/// </summary>
public sealed class DownloadRequestEmailAttachmentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RequestEmailReader emails;
    private readonly IIntakeMessageReader reader;

    public DownloadRequestEmailAttachmentEndpoint(
        SignedInUserResolver users, RequestEmailReader emails, IIntakeMessageReader reader)
    {
        this.users = users;
        this.emails = emails;
        this.reader = reader;
    }

    // Request reads are internal plus the architect, who reads/approves RFIs per the permissions matrix.
    private static readonly RoleSet RolesThatMayReadRequests = JpmsRoleSets.InternalAndArchitect;

    [Function(nameof(DownloadRequestEmailAttachmentEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/messages/email-attachment")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRequests.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var messageId = request.Query["id"].ToString();
        var attachmentId = request.Query["aid"].ToString();
        if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(attachmentId))
            return new BadRequestObjectResult("id and aid are required.");
        var internetMessageId = request.Query["imid"].ToString();

        var tagged = await emails.ForRequestAsync(requestId, cancellationToken);
        var match = tagged.FirstOrDefault(email =>
            string.Equals(email.Id, messageId, StringComparison.Ordinal)
            || (!string.IsNullOrWhiteSpace(internetMessageId)
                && string.Equals(email.InternetMessageId, internetMessageId, StringComparison.Ordinal)));
        if (match is null)
            return new NotFoundObjectResult("That email is not part of this request's conversation.");

        var attachment = await reader.GetAttachmentAsync(match.Id, attachmentId, cancellationToken);
        if (attachment is null)
            return new NotFoundObjectResult(
                "Couldn't download that attachment from the mailbox — it may have been removed, or it isn't a file.");

        // Inline rendering (?inline=1, no Content-Disposition) is honoured ONLY for content a
        // browser can show without executing anything — the embedder's raster whitelist plus PDF.
        // Everything else always downloads: this endpoint navigates on the portal's own origin, so
        // serving an emailed SVG or HTML file inline would run the sender's markup as the reader.
        var isInlineView = request.Query.TryGetValue("inline", out var inlineValue)
            && (inlineValue == "1" || string.Equals(inlineValue, "true", StringComparison.OrdinalIgnoreCase))
            && MayRenderInline(attachment.ContentType);

        request.HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
        var result = new FileContentResult(attachment.Content, attachment.ContentType)
        {
            EnableRangeProcessing = true
        };
        if (!isInlineView)
            result.FileDownloadName = string.IsNullOrWhiteSpace(attachment.Name) ? "attachment" : attachment.Name;
        return result;
    }

    private static bool MayRenderInline(string contentType) =>
        InboundEmailBodyBuilder.EmbeddableImageTypes.Contains(contentType)
        || string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
}
