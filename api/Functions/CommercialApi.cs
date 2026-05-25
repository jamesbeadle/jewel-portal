using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class CommercialApi
{
    private readonly JpmsContext context;

    public CommercialApi(JpmsContext context) { this.context = context; }

    [Function("ListValuations")]
    public async Task<IActionResult> ListValuations(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/valuations")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.Valuations
            .Where(v => v.ProjectId == projectId)
            .OrderByDescending(v => v.IssuedAt ?? DateTimeOffset.MinValue).ToListAsync());

    [Function("UpsertValuation")]
    public async Task<IActionResult> UpsertValuation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "valuations")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<ValuationEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.Valuations.FindAsync(incoming.ValuationId);
        if (existing is null) context.Valuations.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }

    [Function("ListBudgets")]
    public async Task<IActionResult> ListBudgets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/cost-code-budgets")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.CostCodeBudgets.Where(b => b.ProjectId == projectId).ToListAsync());

    [Function("ListTimesheets")]
    public async Task<IActionResult> ListTimesheets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/timesheets")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.Timesheets.Where(t => t.ProjectId == projectId).OrderByDescending(t => t.WorkedOn).ToListAsync());

    [Function("UpsertTimesheet")]
    public async Task<IActionResult> UpsertTimesheet(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "timesheets")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<TimesheetEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.Timesheets.FindAsync(incoming.TimesheetId);
        if (existing is null) context.Timesheets.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }
}
