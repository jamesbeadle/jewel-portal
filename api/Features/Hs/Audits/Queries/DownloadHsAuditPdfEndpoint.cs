using Jewel.JPMS.Api.Features.Hs.Audits.Documents;
using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Audits.Queries;

/// <summary>GET hs-audits/{id}/pdf — the officer's inspection report in the house style, built
/// from the record on every download and never stored, in any status. The portal never emails
/// it: she downloads it and sends it on, as she did with the spreadsheet.</summary>
public sealed class DownloadHsAuditPdfEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetHsAudit, HsAuditView> audits;
    private readonly JpmsContext context;

    public DownloadHsAuditPdfEndpoint(SignedInUserResolver users, IQueryHandler<GetHsAudit, HsAuditView> audits, JpmsContext context)
    {
        this.users = users;
        this.audits = audits;
        this.context = context;
    }

    [Function(nameof(DownloadHsAuditPdf))]
    public async Task<IActionResult> DownloadHsAuditPdf(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hs-audits/{auditId}/pdf")] HttpRequest request,
        string auditId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!HsAuditRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var view = await audits.HandleAsync(new GetHsAudit(auditId), cancellationToken);
        var projectName = await ProjectNameAsync(view.Audit.ProjectId, cancellationToken);
        var pdf = HsAuditPdfRenderer.Render(view, projectName, DateTimeOffset.UtcNow);
        return new FileContentResult(pdf, "application/pdf") { FileDownloadName = HsAuditFileNames.Pdf(view.Audit, projectName) };
    }

    private async Task<string> ProjectNameAsync(string projectId, CancellationToken cancellationToken)
    {
        var project = await context.Projects.AsNoTracking().FirstOrDefaultAsync(row => row.ProjectId == projectId, cancellationToken);
        return project?.Name ?? projectId;
    }
}
