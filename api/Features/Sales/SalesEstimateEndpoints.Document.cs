using Jewel.JPMS.Api.Features.Sales.Documents;

namespace Jewel.JPMS.Api.Features.Sales;

// GET sales/estimates/{estimateId}/document (2026-09-15): the estimate sheet as a PDF, rendered
// from the register on every download — nothing stored. Readers, like every other Sales read.
public sealed partial class SalesEstimateEndpoints
{
    [Function("GetEstimateDocument")]
    public async Task<IActionResult> Document(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sales/estimates/{estimateId}/document")] HttpRequest request,
        string estimateId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SalesRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var estimateEntity = await context.LeadEstimates.AsNoTracking()
            .FirstOrDefaultAsync(row => row.EstimateId == estimateId, cancellationToken);
        if (estimateEntity is null) return new NotFoundObjectResult($"Estimate {estimateId} not found.");
        var leadEntity = await context.Leads.AsNoTracking()
            .FirstOrDefaultAsync(row => row.LeadId == estimateEntity.LeadId, cancellationToken);
        if (leadEntity is null) return new NotFoundObjectResult($"Lead {estimateEntity.LeadId} not found.");

        var estimate = estimateEntity.ToModel();
        var lead = leadEntity.ToModel(null);
        var pdf = EstimateDocumentRenderer.Render(new EstimateDocumentRenderer.Model(estimate, lead, DateTimeOffset.UtcNow));
        return new FileContentResult(pdf, "application/pdf") { FileDownloadName = EstimateDocumentRenderer.FileName(estimate, lead) };
    }
}
