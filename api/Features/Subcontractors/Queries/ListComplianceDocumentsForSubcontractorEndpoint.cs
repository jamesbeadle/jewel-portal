using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListComplianceDocumentsForSubcontractorEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListComplianceDocumentsForSubcontractor, IReadOnlyList<ComplianceDocument>> handler;

    public ListComplianceDocumentsForSubcontractorEndpoint(SignedInUserResolver users, IQueryHandler<ListComplianceDocumentsForSubcontractor, IReadOnlyList<ComplianceDocument>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListComplianceDocumentsForSubcontractor))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subcontractors/{subcontractorId}/compliance")] HttpRequest request,
        string subcontractorId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        if (!DirectoryRoles.AllowedToReadCompliance.IncludesAny(signedInUser.Roles))
        {
            var ownSubcontractorId = SubcontractorScope.OwnSubcontractorId(signedInUser);
            if (ownSubcontractorId is null
                || !string.Equals(ownSubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase))
                return new StatusCodeResult(403);
        }

        return new OkObjectResult(await handler.HandleAsync(new ListComplianceDocumentsForSubcontractor(subcontractorId), request.HttpContext.RequestAborted));
    }
}
