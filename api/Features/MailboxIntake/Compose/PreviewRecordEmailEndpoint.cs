using Jewel.JPMS.Contracts.MailboxCompose;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

/// <summary>
/// Reading a record's email before it is sent. The gate is the SEND gate for that kind of record,
/// taken from the composer itself: previewing an email you could not send would be reading
/// correspondence that is not yours.
/// </summary>
public sealed class PreviewRecordEmailEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecordEmailComposers composers;
    private readonly IQueryHandler<PreviewRecordEmail, RecordEmailPreview> handler;

    public PreviewRecordEmailEndpoint(
        SignedInUserResolver users,
        RecordEmailComposers composers,
        IQueryHandler<PreviewRecordEmail, RecordEmailPreview> handler)
    {
        this.users = users;
        this.composers = composers;
        this.handler = handler;
    }

    [Function(nameof(PreviewRecordEmail))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "record-emails/{record}/{recordId}/preview")]
        HttpRequest request,
        string record,
        string recordId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        if (!Enum.TryParse<RecordType>(record, ignoreCase: true, out var kind))
            return new BadRequestObjectResult($"'{record}' is not a kind of record.");

        var composer = composers.Find(kind);
        if (composer is null)
            return new BadRequestObjectResult(
                $"The portal doesn't compose an email for a {kind}, so there is nothing to preview.");
        if (!composer.RolesThatMaySend.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var recipientOverride = request.Query["recipientOverride"].ToString();
        var query = new PreviewRecordEmail(
            kind, recordId, string.IsNullOrWhiteSpace(recipientOverride) ? null : recipientOverride.Trim());
        return new OkObjectResult(await handler.HandleAsync(query, request.HttpContext.RequestAborted));
    }
}
