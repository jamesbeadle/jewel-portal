using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Queries;

/// <summary>GET contractors-reports/{id}/pdf — built from the register on every download, nothing
/// stored; a report the wording gate refuses answers 422 with the findings.</summary>
public sealed class DownloadContractorsReportEndpoints
{
    private const int UnprocessableEntity = 422;
    private readonly SignedInUserResolver users;
    private readonly ContractorsReportBuilder builder;

    public DownloadContractorsReportEndpoints(SignedInUserResolver users, ContractorsReportBuilder builder)
    {
        this.users = users;
        this.builder = builder;
    }

    [Function(nameof(DownloadContractorsReportPdf))]
    public async Task<IActionResult> DownloadContractorsReportPdf(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "contractors-reports/{contractorsReportId}/pdf")] HttpRequest request,
        string contractorsReportId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProgressRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var outcome = await builder.BuildAsync(contractorsReportId, cancellationToken);
        if (outcome is null) return new NotFoundObjectResult($"Contractor's Report {contractorsReportId} not found.");
        if (outcome.IsRefused) return new ObjectResult(outcome.Findings) { StatusCode = UnprocessableEntity };

        var file = outcome.File!;
        return new FileContentResult(file.Content, file.ContentType) { FileDownloadName = file.FileName };
    }
}
