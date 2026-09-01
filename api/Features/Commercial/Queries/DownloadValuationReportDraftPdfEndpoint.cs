using Jewel.JPMS.Api.Features.Commercial.Documents;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

/// <summary>
/// GET /api/projects/{projectId}/valuation-report/pdf — renders and streams the LIVE valuation
/// report as a working-copy PDF: the same branded statement a snapshot produces, computed by the
/// same capture maths (latest claim, certified from issued/paid invoices), but stamped
/// "WORKING COPY — NOT AN ISSUED STATEMENT" throughout and never persisted. This is the
/// review-before-you-claim export the accountant asked for; the client-facing record remains the
/// frozen snapshot behind the invoice.
/// </summary>
public sealed class DownloadValuationReportDraftPdfEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationReportSnapshotPdfBuilder builder;

    public DownloadValuationReportDraftPdfEndpoint(
        SignedInUserResolver users,
        ValuationReportSnapshotPdfBuilder builder)
    {
        this.users = users; this.builder = builder;
    }

    // Commercial reads are internal-only; external portal logins have no view of project money.
    // Mirrors DownloadValuationReportSnapshotPdfEndpoint — this is the same report, one stage earlier.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(DownloadValuationReportDraftPdfEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/valuation-report/pdf")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        ValuationReportSnapshotPdf pdf;
        try
        {
            pdf = await builder.BuildDraftAsync(projectId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return new NotFoundObjectResult(ex.Message);
        }

        return new FileContentResult(pdf.Content, "application/pdf") { FileDownloadName = pdf.FileName };
    }
}
