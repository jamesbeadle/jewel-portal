using Jewel.JPMS.Api.Features.Commercial.Documents;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

/// <summary>
/// GET /api/valuation-report-snapshots/{snapshotId}/pdf — renders and streams the frozen
/// valuation report as the branded statement PDF. The snapshot is immutable, so the PDF is the
/// same on every download; nothing is stored. The email-draft command attaches the same
/// rendering (via the shared builder), so what's downloaded and what's sent never diverge.
/// </summary>
public sealed class DownloadValuationReportSnapshotPdfEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationReportSnapshotPdfBuilder builder;

    public DownloadValuationReportSnapshotPdfEndpoint(
        SignedInUserResolver users,
        ValuationReportSnapshotPdfBuilder builder)
    {
        this.users = users; this.builder = builder;
    }

    // Commercial reads are internal-only; external portal logins have no view of project money.
    // Mirrors GetValuationReportSnapshotEndpoint — the PDF shows exactly what that query returns.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(DownloadValuationReportSnapshotPdfEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "valuation-report-snapshots/{snapshotId}/pdf")] HttpRequest request,
        string snapshotId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        ValuationReportSnapshotPdf pdf;
        try
        {
            pdf = await builder.BuildAsync(snapshotId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return new NotFoundObjectResult(ex.Message);
        }

        return new FileContentResult(pdf.Content, "application/pdf") { FileDownloadName = pdf.FileName };
    }
}
