using Jewel.JPMS.Api.Features.Commercial.Documents;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

/// <summary>
/// GET /api/projects/{projectId}/valuation-report/pdf — renders and streams the project's LATEST
/// valuation as its statement: a Draft prints as the working copy, stamped "WORKING COPY — NOT AN
/// ISSUED STATEMENT" throughout and never persisted (the review-before-you-claim export the
/// accountant asked for); a locked latest claim prints its frozen statement. Nothing is stored.
/// </summary>
public sealed class DownloadValuationReportDraftPdfEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationStatementPdfBuilder builder;

    public DownloadValuationReportDraftPdfEndpoint(
        SignedInUserResolver users,
        ValuationStatementPdfBuilder builder)
    {
        this.users = users; this.builder = builder;
    }

    // Commercial reads are internal-only; external portal logins have no view of project money.
    // Mirrors DownloadValuationStatementPdfEndpoint — the same report, addressed by project.
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

        ValuationStatementPdf pdf;
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
