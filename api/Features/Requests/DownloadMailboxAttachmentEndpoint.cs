using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Gates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests;

/// <summary>
/// GET /api/mailbox/message/attachment?id={messageId}&amp;aid={attachmentId} — streams one
/// attachment of a triaged email so a triager can check its contents before acting on it (e.g.
/// before saving it into a project's drawings). Ids travel in the query string, never the route
/// path, because Graph ids contain characters that don't survive a URL path segment. The bytes are
/// proxied through the API on demand — nothing is stored in JPMS. Gated to the triage roles, like
/// every other mailbox read.
/// </summary>
public sealed class DownloadMailboxAttachmentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IIntakeMessageReader reader;

    public DownloadMailboxAttachmentEndpoint(SignedInUserResolver users, IIntakeMessageReader reader)
    {
        this.users = users;
        this.reader = reader;
    }

    [Function(nameof(DownloadMailboxAttachmentEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mailbox/message/attachment")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var messageId = request.Query["id"].ToString();
        var attachmentId = request.Query["aid"].ToString();
        if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(attachmentId))
            return new BadRequestObjectResult("id and aid are required.");

        var attachment = await reader.GetAttachmentAsync(messageId, attachmentId, cancellationToken);
        if (attachment is null)
            return new NotFoundObjectResult(
                "Couldn't download that attachment from the mailbox — it may have been removed, or it isn't a file.");

        // Same inline/download split as the drawing file endpoint: inline requests (?inline=1) come
        // from the in-app preview iframe, so Content-Disposition stays unset and the browser renders
        // the file in place; explicit downloads get a filename to force the attachment behaviour.
        var inline = request.Query.TryGetValue("inline", out var inlineValue)
            && (inlineValue == "1" || string.Equals(inlineValue, "true", StringComparison.OrdinalIgnoreCase));

        var result = new FileContentResult(attachment.Content, attachment.ContentType)
        {
            EnableRangeProcessing = true
        };
        if (!inline)
            result.FileDownloadName = string.IsNullOrWhiteSpace(attachment.Name) ? "attachment" : attachment.Name;
        return result;
    }
}
