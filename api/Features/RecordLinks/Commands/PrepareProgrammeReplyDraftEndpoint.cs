using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Commands;

/// <summary>
/// POST /api/projects/{projectId}/programme/emails/reply-draft — stage the written reply as an
/// Outlook draft (in the original conversation thread) on a programme-tagged email. JSON body: the
/// <see cref="PrepareProgrammeReplyDraft"/> command; the route's projectId wins over the body's.
/// Nothing is sent — the draft waits in the projects mailbox's Drafts folder.
/// </summary>
public sealed class PrepareProgrammeReplyDraftEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<PrepareProgrammeReplyDraft, ProgrammeReplyDraft> handler;
    private readonly Audit.AuditActor auditActor;

    public PrepareProgrammeReplyDraftEndpoint(
        SignedInUserResolver users,
        ICommandHandler<PrepareProgrammeReplyDraft, ProgrammeReplyDraft> handler,
        Audit.AuditActor auditActor)
    {
        this.users = users;
        this.handler = handler;
        this.auditActor = auditActor;
    }

    // A reply draft stages an external communication in the shared mailbox — the same act as the
    // request-reply draft, so the same shape of gate: the roles that speak for the project
    // (directors, project managers, site managers; admins carry every role server-side). The
    // architect — present on the request gate for RFIs — has no programme-communications surface,
    // so is deliberately absent here.
    private static readonly RoleSet RolesThatMayDraft =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);

    [Function(nameof(PrepareProgrammeReplyDraft))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/programme/emails/reply-draft")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayDraft.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        // Attribute the handler's DraftCreated audit row to whoever pressed the button.
        auditActor.Email = signedInUser.Email;

        PrepareProgrammeReplyDraft? body = null;
        try { body = await request.ReadFromJsonAsync<PrepareProgrammeReplyDraft>(); }
        catch { /* the checks below report what's missing */ }
        if (body is null || string.IsNullOrWhiteSpace(body.MessageId))
            return new BadRequestObjectResult("messageId is required.");
        if (string.IsNullOrWhiteSpace(body.ReplyBody))
            return new BadRequestObjectResult("Write the reply before creating the draft.");

        var command = body with { ProjectId = projectId };

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // A vanished original email / unconfigured mailbox are user-fixable — surface verbatim.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
