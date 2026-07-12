using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ReconciliationPackageQueryEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListReconciliationPackagesForProject, IReadOnlyList<ReconciliationPackage>> definitionsHandler;
    private readonly IQueryHandler<ListPackageReconciliation, IReadOnlyList<PackageReconciliationRow>> reportHandler;

    public ReconciliationPackageQueryEndpoints(
        SignedInUserResolver users,
        IQueryHandler<ListReconciliationPackagesForProject, IReadOnlyList<ReconciliationPackage>> definitionsHandler,
        IQueryHandler<ListPackageReconciliation, IReadOnlyList<PackageReconciliationRow>> reportHandler)
    {
        this.users = users;
        this.definitionsHandler = definitionsHandler;
        this.reportHandler = reportHandler;
    }

    [Function(nameof(ListReconciliationPackagesForProject))]
    public async Task<IActionResult> Definitions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/reconciliation-packages")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var packages = await definitionsHandler.HandleAsync(
            new ListReconciliationPackagesForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(packages);
    }

    [Function(nameof(ListPackageReconciliation))]
    public async Task<IActionResult> Report(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/package-reconciliation")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var rows = await reportHandler.HandleAsync(
            new ListPackageReconciliation(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(rows);
    }
}
