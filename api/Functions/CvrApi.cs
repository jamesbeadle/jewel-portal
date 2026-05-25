using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class CvrApi
{
    private readonly JpmsContext context;

    public CvrApi(JpmsContext context) { this.context = context; }

    [Function("ListSnapshots")]
    public async Task<IActionResult> ListSnapshots(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/cvr-snapshots")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.CvrSnapshots
            .Where(s => s.ProjectId == projectId).OrderByDescending(s => s.SnapshotAt).ToListAsync());

    [Function("ListForecastComponents")]
    public async Task<IActionResult> ListForecast(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/forecast-components")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.ForecastComponents.Where(f => f.ProjectId == projectId).ToListAsync());

    [Function("ListAccruals")]
    public async Task<IActionResult> ListAccruals(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/qs-accruals")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.QsAccruals
            .Where(a => a.ProjectId == projectId).OrderByDescending(a => a.SignedOffAt).ToListAsync());

    [Function("UpsertAccrual")]
    public async Task<IActionResult> UpsertAccrual(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "qs-accruals")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<QsAccrualEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.QsAccruals.FindAsync(incoming.QsAccrualId);
        if (existing is null) context.QsAccruals.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }

    [Function("ListPrelims")]
    public async Task<IActionResult> ListPrelims(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/prelims")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.PrelimItems.Where(p => p.ProjectId == projectId).ToListAsync());

    [Function("ListPrelimEntries")]
    public async Task<IActionResult> ListPrelimEntries(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "prelims/{prelimItemId}/entries")] HttpRequest request,
        string prelimItemId) =>
        new OkObjectResult(await context.PrelimForecastEntries.Where(p => p.PrelimItemId == prelimItemId).OrderBy(p => p.WeekNumber).ToListAsync());

    [Function("ListEots")]
    public async Task<IActionResult> ListEots(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/eots")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.Eots.Where(e => e.ProjectId == projectId).OrderByDescending(e => e.GrantedAt).ToListAsync());

    [Function("UpsertEot")]
    public async Task<IActionResult> UpsertEot(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "eots")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<EotEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.Eots.FindAsync(incoming.EotId);
        if (existing is null) context.Eots.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }
}
