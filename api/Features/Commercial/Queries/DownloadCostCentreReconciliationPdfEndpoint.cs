using Jewel.JPMS.Api.Features.Commercial.Documents;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

/// <summary>
/// Streams one cost centre's (or roll-up group's) reconciliation as a branded PDF — the
/// delivery position the accountant sends the managing director. A plain GET so the UI can
/// link to it directly (the session cookie authenticates, as with the statement and snapshot
/// PDFs); the centre's codes travel comma-separated in the query string because a roll-up
/// group has several and route templates take one value.
/// </summary>
public sealed class DownloadCostCentreReconciliationPdfEndpoint
{
    // Commercial reads are internal-only, matching the valuation snapshot PDF.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    private readonly SignedInUserResolver users;
    private readonly CostCentreReconciliationPdfBuilder builder;

    public DownloadCostCentreReconciliationPdfEndpoint(
        SignedInUserResolver users,
        CostCentreReconciliationPdfBuilder builder)
    {
        this.users = users;
        this.builder = builder;
    }

    [Function(nameof(DownloadCostCentreReconciliationPdfEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/cost-centre-reconciliation/pdf")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(403);

        var codes = (request.Query["codes"].ToString() ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        if (codes.Count == 0) return new BadRequestObjectResult("At least one cost code is required.");

        var heading = request.Query["heading"].ToString();
        if (string.IsNullOrWhiteSpace(heading)) heading = string.Join(", ", codes);

        try
        {
            var pdf = await builder.BuildAsync(projectId, codes, heading, cancellationToken);
            return new FileContentResult(pdf.Content, "application/pdf") { FileDownloadName = pdf.FileName };
        }
        catch (InvalidOperationException ex)
        {
            return new NotFoundObjectResult(ex.Message);
        }
    }
}
