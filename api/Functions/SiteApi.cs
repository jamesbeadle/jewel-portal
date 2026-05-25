using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class SiteApi
{
    private readonly JpmsContext context;

    public SiteApi(JpmsContext context) { this.context = context; }

    [Function("ListSiteReports")]
    public async Task<IActionResult> ListReports(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/site-reports")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.SiteReports.Where(r => r.ProjectId == projectId).OrderByDescending(r => r.PeriodEnd).ToListAsync());

    [Function("UpsertSiteReport")]
    public async Task<IActionResult> UpsertReport(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "site-reports")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<SiteReportEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.SiteReports.FindAsync(incoming.SiteReportId);
        if (existing is null) context.SiteReports.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }

    [Function("ListProgrammeTasks")]
    public async Task<IActionResult> ListTasks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/programme")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.ProgrammeTasks.Where(t => t.ProjectId == projectId).OrderBy(t => t.PlannedStart).ToListAsync());

    [Function("UpsertProgrammeTask")]
    public async Task<IActionResult> UpsertTask(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "programme-tasks")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<ProgrammeTaskEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.ProgrammeTasks.FindAsync(incoming.ProgrammeTaskId);
        if (existing is null) context.ProgrammeTasks.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }
}
