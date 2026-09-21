using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListCurrentComplianceDocumentsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCurrentComplianceDocuments, IReadOnlyList<ComplianceDocument>> handler;

    public ListCurrentComplianceDocumentsEndpoint(SignedInUserResolver users, IQueryHandler<ListCurrentComplianceDocuments, IReadOnlyList<ComplianceDocument>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListCurrentComplianceDocuments))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "compliance-documents")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!DirectoryRoles.AllowedToReadCompliance.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        return new OkObjectResult(await handler.HandleAsync(new ListCurrentComplianceDocuments(), request.HttpContext.RequestAborted));
    }
}
