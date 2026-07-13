using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

// Anonymous, token-authenticated (SiteAccessGate) — the page behind the project QR code.
// This surface must never serialise rates or £; SiteSignInSheet is a hours-only shape.

public sealed class GetSiteSignInSheetEndpoint
{
    private readonly SiteAccessGate gate;
    private readonly IQueryHandler<GetSiteSignInSheet, SiteSignInSheet> handler;
    public GetSiteSignInSheetEndpoint(SiteAccessGate gate, IQueryHandler<GetSiteSignInSheet, SiteSignInSheet> handler)
    { this.gate = gate; this.handler = handler; }

    [Function(nameof(GetSiteSignInSheet))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "site-labour/{token}")] HttpRequest request, string token)
    {
        var access = await gate.ResolveAsync(token, request.HttpContext.RequestAborted);
        if (access is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new GetSiteSignInSheet(token), request.HttpContext.RequestAborted));
    }
}

public sealed class GetSiteSignInSheetHandler : IQueryHandler<GetSiteSignInSheet, SiteSignInSheet>
{
    private readonly JpmsContext context;
    private readonly SiteAccessGate gate;
    public GetSiteSignInSheetHandler(JpmsContext context, SiteAccessGate gate) { this.context = context; this.gate = gate; }

    public async Task<SiteSignInSheet> HandleAsync(GetSiteSignInSheet query, CancellationToken cancellationToken)
    {
        var access = await gate.ResolveAsync(query.Token, cancellationToken)
            ?? throw new InvalidOperationException("Site access token is not valid.");

        var project = await context.Projects.FindAsync(new object[] { access.ProjectId }, cancellationToken)
            ?? throw new InvalidOperationException($"Project {access.ProjectId} not found.");

        var today = SiteClock.Today();

        var workers = await context.ProjectWorkerAssignments
            .Where(assignment => assignment.ProjectId == access.ProjectId && assignment.IsActive)
            .Join(context.Workers, assignment => assignment.WorkerId, worker => worker.WorkerId,
                (assignment, worker) => new { worker.WorkerId, worker.Name, worker.IsActive })
            .Where(worker => worker.IsActive)
            .OrderBy(worker => worker.Name)
            .ToListAsync(cancellationToken);

        var workerIds = workers.Select(worker => worker.WorkerId).ToList();

        var attendanceToday = await context.SiteAttendances
            .Where(attendance => attendance.ProjectId == access.ProjectId
                                 && attendance.WorkDate == today
                                 && workerIds.Contains(attendance.WorkerId))
            .ToListAsync(cancellationToken);

        var rejectedCounts = await context.Timesheets
            .Where(timesheet => timesheet.ProjectId == access.ProjectId
                                && timesheet.Status == (int)TimesheetStatus.Rejected
                                && workerIds.Contains(timesheet.WorkerId))
            .GroupBy(timesheet => timesheet.WorkerId)
            .Select(group => new { WorkerId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var rejectedByWorker = rejectedCounts.ToDictionary(entry => entry.WorkerId, entry => entry.Count);

        var sheetWorkers = workers.Select(worker =>
        {
            var attendance = attendanceToday.FirstOrDefault(row => row.WorkerId == worker.WorkerId);
            return new SiteSheetWorker(
                worker.WorkerId, worker.Name,
                attendance is not null,
                attendance?.SignedOutAt is not null,
                rejectedByWorker.TryGetValue(worker.WorkerId, out var count) ? count : 0);
        }).ToList();

        // The allocation list: cost codes budgeted for this project; if nothing is budgeted yet,
        // fall back to the whole active master list so capture never dead-ends on site.
        var budgetedCodes = await context.CostCodeBudgets
            .Where(budget => budget.ProjectId == access.ProjectId)
            .Select(budget => budget.CostCode)
            .ToListAsync(cancellationToken);

        var centres = await context.CostCenters
            .Where(centre => centre.IsActive)
            .OrderBy(centre => centre.SortOrder)
            .Select(centre => new { centre.Code, centre.Name })
            .ToListAsync(cancellationToken);

        var codes = budgetedCodes.Count > 0
            ? centres.Where(centre => budgetedCodes.Contains(centre.Code)).ToList()
            : centres;

        return new SiteSignInSheet(
            access.ProjectId, project.Name,
            sheetWorkers,
            codes.Select(centre => new SiteSheetCostCode(centre.Code, centre.Name)).ToList());
    }
}
