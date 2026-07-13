using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

// Anonymous, token-authenticated (SiteAccessGate): the worker's re-opened (rejected) days.
// WorkerTimesheetView is hours-only — no rates, no £, ever, on this surface.

public sealed class ListWorkerRejectedTimesheetsEndpoint
{
    private readonly SiteAccessGate gate;
    private readonly IQueryHandler<ListWorkerRejectedTimesheets, IReadOnlyList<WorkerTimesheetView>> handler;
    public ListWorkerRejectedTimesheetsEndpoint(SiteAccessGate gate, IQueryHandler<ListWorkerRejectedTimesheets, IReadOnlyList<WorkerTimesheetView>> handler)
    { this.gate = gate; this.handler = handler; }

    [Function(nameof(ListWorkerRejectedTimesheets))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "site-labour/{token}/workers/{workerId}/rejected")] HttpRequest request, string token, string workerId)
    {
        var access = await gate.ResolveAsync(token, request.HttpContext.RequestAborted);
        if (access is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListWorkerRejectedTimesheets(token, workerId), request.HttpContext.RequestAborted));
    }
}

public sealed class ListWorkerRejectedTimesheetsHandler : IQueryHandler<ListWorkerRejectedTimesheets, IReadOnlyList<WorkerTimesheetView>>
{
    private readonly JpmsContext context;
    private readonly SiteAccessGate gate;
    public ListWorkerRejectedTimesheetsHandler(JpmsContext context, SiteAccessGate gate) { this.context = context; this.gate = gate; }

    public async Task<IReadOnlyList<WorkerTimesheetView>> HandleAsync(ListWorkerRejectedTimesheets query, CancellationToken cancellationToken)
    {
        var access = await gate.ResolveAsync(query.Token, cancellationToken)
            ?? throw new InvalidOperationException("Site access token is not valid.");

        var rejected = await context.Timesheets
            .Where(timesheet => timesheet.ProjectId == access.ProjectId
                                && timesheet.WorkerId == query.WorkerId
                                && timesheet.Status == (int)TimesheetStatus.Rejected)
            .OrderByDescending(timesheet => timesheet.WorkedOn)
            .ToListAsync(cancellationToken);

        return rejected.Select(timesheet => timesheet.ToWorkerView()).ToList();
    }
}
