using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class GetSubcontractorStatementEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetSubcontractorStatement, SubcontractorStatement> handler;

    public GetSubcontractorStatementEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetSubcontractorStatement, SubcontractorStatement> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // A statement of account is commercial correspondence: the internal commercial side (directors,
    // finance, PMs, the QS, office/compliance) may read it; portal logins never see the directory's
    // cross-project figures.
    internal static readonly RoleSet RolesThatMayReadStatements = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator);

    [Function(nameof(GetSubcontractorStatement))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subcontractors/{subcontractorId}/statement")] HttpRequest request,
        string subcontractorId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadStatements.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(
                new GetSubcontractorStatement(subcontractorId), request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new NotFoundObjectResult(ex.Message);
        }
    }
}
