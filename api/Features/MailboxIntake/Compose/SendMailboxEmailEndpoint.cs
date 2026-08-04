using System.Text.Json;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.MailboxCompose;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

/// <summary>
/// POST /api/mailbox/compose — send (or stage) an email from the projects mailbox. Two request
/// shapes on one route:
///   • application/json — the <see cref="SendMailboxEmail"/> command alone (no uploaded files).
///   • multipart/form-data — a "command" part carrying the same JSON, plus one file part per
///     Source=Upload attachment, matched by part name to <see cref="ComposeAttachmentRef.Id"/>
///     (the same transport shape as the progress-photo upload).
/// Gated to the triage roles; SenderEmail is stamped from the signed-in user — the client cannot
/// spoof it. Handler-refused sends (validation, wall, mailbox unavailable) surface verbatim as 400s
/// so the composer shows them inline rather than as a toast.
/// </summary>
public sealed class SendMailboxEmailEndpoint
{
    // Per-file cap for uploads; the handler separately caps the email's combined attachment size.
    private const long MaxUploadBytes = 25_000_000;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly SignedInUserResolver users;
    private readonly Audit.AuditActor auditActor;
    private readonly SendMailboxEmailHandler handler;

    public SendMailboxEmailEndpoint(SignedInUserResolver users, Audit.AuditActor auditActor, SendMailboxEmailHandler handler)
    {
        this.users = users;
        this.auditActor = auditActor;
        this.handler = handler;
    }

    [Function(nameof(SendMailboxEmail))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/compose")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        auditActor.Email = signedInUser.Email;

        SendMailboxEmail? command;
        Dictionary<string, SendMailboxEmailHandler.UploadedFile>? uploads = null;

        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var commandJson = form["command"].ToString();
            if (string.IsNullOrWhiteSpace(commandJson))
                return new BadRequestObjectResult("The compose request is missing its command part.");
            try { command = JsonSerializer.Deserialize<SendMailboxEmail>(commandJson, Json); }
            catch { command = null; }

            uploads = new Dictionary<string, SendMailboxEmailHandler.UploadedFile>(StringComparer.Ordinal);
            foreach (var file in form.Files)
            {
                if (file.Length == 0) continue;
                if (file.Length > MaxUploadBytes)
                    return new BadRequestObjectResult($"{file.FileName} is larger than 25 MB — attach a smaller file.");
                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer, cancellationToken);
                uploads[file.Name] = new SendMailboxEmailHandler.UploadedFile(
                    string.IsNullOrWhiteSpace(file.FileName) ? "attachment" : file.FileName,
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    buffer.ToArray());
            }
        }
        else
        {
            try { command = await request.ReadFromJsonAsync<SendMailboxEmail>(cancellationToken); }
            catch { command = null; }
        }

        if (command is null)
            return new BadRequestObjectResult("The compose request couldn't be read.");

        // The sender is always the signed-in user.
        command = command with { SenderEmail = signedInUser.Email };

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, uploads, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // Validation answers and user-fixable mailbox conditions — the composer shows these
            // inline next to the fields (400s never toast).
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
