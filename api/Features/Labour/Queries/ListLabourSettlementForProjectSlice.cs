using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

// The settlement reconciliation view (scope §6): per subcontractor, approved timesheet £ vs
// invoices marked covered-by-timesheets vs posted variances. UnresolvedVariance =
// covered-invoice £ − approved £ − posted variances; non-zero rows are the accountant's
// worklist and stay open until closed by one of the four resolution paths.

public sealed class ListLabourSettlementForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListLabourSettlementForProject, IReadOnlyList<LabourSettlementRow>> handler;
    public ListLabourSettlementForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListLabourSettlementForProject, IReadOnlyList<LabourSettlementRow>> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ListLabourSettlementForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/labour/settlement")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageSettlement.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListLabourSettlementForProject(projectId), request.HttpContext.RequestAborted));
    }
}

public sealed class ListLabourSettlementForProjectHandler : IQueryHandler<ListLabourSettlementForProject, IReadOnlyList<LabourSettlementRow>>
{
    private readonly JpmsContext context;
    public ListLabourSettlementForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<LabourSettlementRow>> HandleAsync(ListLabourSettlementForProject query, CancellationToken cancellationToken)
    {
        // Approved labour £ per subcontractor (via the worker's company).
        var approvedRows = await context.Timesheets
            .Where(timesheet => timesheet.ProjectId == query.ProjectId
                                && timesheet.Status == (int)TimesheetStatus.Approved
                                && timesheet.WorkerId != "")
            .Join(context.Workers, timesheet => timesheet.WorkerId, worker => worker.WorkerId,
                (timesheet, worker) => new { SubcontractorId = worker.SubcontractorId ?? "", timesheet.CostAmount })
            .GroupBy(row => row.SubcontractorId)
            .Select(group => new { SubcontractorId = group.Key, Amount = group.Sum(row => row.CostAmount) })
            .ToListAsync(cancellationToken);

        // Invoice £ marked covered-by-timesheets per subcontractor.
        var coveredRows = await context.XeroLineTimesheetCovers
            .Where(cover => cover.ProjectId == query.ProjectId)
            .Join(context.XeroLedgerLines, cover => cover.XeroLedgerLineId, line => line.XeroLedgerLineId,
                (cover, line) => new { cover.SubcontractorId, Amount = line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net })
            .GroupBy(row => row.SubcontractorId)
            .Select(group => new { SubcontractorId = group.Key, Amount = group.Sum(row => row.Amount) })
            .ToListAsync(cancellationToken);

        var varianceRows = await context.LabourSettlementVariances
            .Where(variance => variance.ProjectId == query.ProjectId)
            .GroupBy(variance => variance.SubcontractorId)
            .Select(group => new { SubcontractorId = group.Key, Amount = group.Sum(variance => variance.Amount) })
            .ToListAsync(cancellationToken);

        var subcontractorIds = approvedRows.Select(row => row.SubcontractorId)
            .Concat(coveredRows.Select(row => row.SubcontractorId))
            .Concat(varianceRows.Select(row => row.SubcontractorId))
            .Where(id => id != "")
            .Distinct().ToList();
        var names = await context.Subcontractors
            .Where(subcontractor => subcontractorIds.Contains(subcontractor.SubcontractorId))
            .ToDictionaryAsync(subcontractor => subcontractor.SubcontractorId,
                subcontractor => subcontractor.CompanyName, cancellationToken);

        var approvedBySub = approvedRows.ToDictionary(row => row.SubcontractorId, row => row.Amount);
        var coveredBySub = coveredRows.ToDictionary(row => row.SubcontractorId, row => row.Amount);
        var varianceBySub = varianceRows.ToDictionary(row => row.SubcontractorId, row => row.Amount);

        var allIds = approvedBySub.Keys.Concat(coveredBySub.Keys).Concat(varianceBySub.Keys).Distinct();
        return allIds.Select(id =>
        {
            var approvedAmount = approvedBySub.TryGetValue(id, out var approvedValue) ? approvedValue : 0m;
            var coveredAmount = coveredBySub.TryGetValue(id, out var coveredValue) ? coveredValue : 0m;
            var varianceAmount = varianceBySub.TryGetValue(id, out var varianceValue) ? varianceValue : 0m;
            return new LabourSettlementRow(
                id,
                id == "" ? "(no subcontractor)" : names.TryGetValue(id, out var name) ? name : id,
                approvedAmount, coveredAmount, varianceAmount,
                coveredAmount - approvedAmount - varianceAmount);
        })
        .OrderBy(row => row.SubcontractorName)
        .ToList();
    }
}
