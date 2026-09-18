using Jewel.JPMS.Api.Features.Commercial.Documents;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

/// <summary>
/// GET /api/valuation-claims/{claimId}/statement/pdf — renders and streams the valuation's
/// statement as the branded PDF: a locked claim's frozen rows (the same on every download;
/// nothing is stored), or a Draft's working copy with its stamps. The email command attaches the
/// same rendering (via the shared builder), so what's downloaded and what's sent never diverge.
/// GET /api/valuation-report-snapshots/{snapshotId}/pdf is the retired address (2026-09-18).
/// </summary>
public sealed class DownloadValuationStatementPdfEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationStatementPdfBuilder builder;

    public DownloadValuationStatementPdfEndpoint(SignedInUserResolver users, ValuationStatementPdfBuilder builder)
    {
        this.users = users; this.builder = builder;
    }

    // Commercial reads are internal-only; external portal logins have no view of project money.
    // Mirrors GetValuationStatementEndpoint — the PDF shows exactly what that query returns.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(DownloadValuationStatementPdfEndpoint))]
    public Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "valuation-claims/{claimId}/statement/pdf")] HttpRequest request,
        string claimId) => RespondAsync(request, claimId);

    [Function("DownloadValuationStatementPdfByRetiredSnapshotId")]
    public Task<IActionResult> RunLegacy(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "valuation-report-snapshots/{snapshotId}/pdf")] HttpRequest request,
        string snapshotId) => RespondAsync(request, snapshotId);

    private async Task<IActionResult> RespondAsync(HttpRequest request, string id)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        ValuationStatementPdf pdf;
        try
        {
            pdf = await builder.BuildAsync(id, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return new NotFoundObjectResult(ex.Message);
        }

        return new FileContentResult(pdf.Content, "application/pdf") { FileDownloadName = pdf.FileName };
    }
}
