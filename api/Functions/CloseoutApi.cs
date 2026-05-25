using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class CloseoutApi
{
    private readonly JpmsContext context;

    public CloseoutApi(JpmsContext context) { this.context = context; }

    [Function("ListDefects")]
    public async Task<IActionResult> ListDefects(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/defects")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.Defects.Where(d => d.ProjectId == projectId).OrderByDescending(d => d.RaisedAt).ToListAsync());

    [Function("UpsertDefect")]
    public async Task<IActionResult> UpsertDefect(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "defects")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<DefectEntity>();
        if (incoming is null) return new BadRequestResult();
        return await Replace(context.Defects, incoming, incoming.DefectId);
    }

    [Function("GetSettlement")]
    public async Task<IActionResult> GetSettlement(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/settlement")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.SettlementRecords.FirstOrDefaultAsync(s => s.ProjectId == projectId));

    [Function("UpsertSettlement")]
    public async Task<IActionResult> UpsertSettlement(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "settlements")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<SettlementRecordEntity>();
        if (incoming is null) return new BadRequestResult();
        return await Replace(context.SettlementRecords, incoming, incoming.SettlementRecordId);
    }

    [Function("GetVat")]
    public async Task<IActionResult> GetVat(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/vat")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.VatAnalyses.FirstOrDefaultAsync(v => v.ProjectId == projectId));

    [Function("UpsertVat")]
    public async Task<IActionResult> UpsertVat(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "vat-analyses")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<VatAnalysisEntity>();
        if (incoming is null) return new BadRequestResult();
        return await Replace(context.VatAnalyses, incoming, incoming.VatAnalysisId);
    }

    [Function("UpsertRetention")]
    public async Task<IActionResult> UpsertRetention(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "retention-releases")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<RetentionReleaseEntity>();
        if (incoming is null) return new BadRequestResult();
        return await Replace(context.RetentionReleases, incoming, incoming.RetentionReleaseId);
    }

    private async Task<IActionResult> Replace<T>(DbSet<T> set, T incoming, object key) where T : class
    {
        var existing = await set.FindAsync(key);
        if (existing is null) set.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }
}
