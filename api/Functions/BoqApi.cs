using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class BoqApi
{
    private readonly JpmsContext context;

    public BoqApi(JpmsContext context) { this.context = context; }

    [Function("ListBoqLines")]
    public async Task<IActionResult> ListLines(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/boq")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.BoqLineItems.Where(l => l.ProjectId == projectId).ToListAsync());

    [Function("UpsertBoqLine")]
    public async Task<IActionResult> Upsert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "boq")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<BoqLineItemEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.BoqLineItems.FindAsync(incoming.BoqLineItemId);
        if (existing is null) context.BoqLineItems.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }

    [Function("DeleteBoqLine")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "boq/{boqLineItemId}")] HttpRequest request,
        string boqLineItemId)
    {
        var existing = await context.BoqLineItems.FindAsync(boqLineItemId);
        if (existing is null) return new NotFoundResult();
        context.BoqLineItems.Remove(existing);
        await context.SaveChangesAsync();
        return new NoContentResult();
    }

    [Function("GetBoqSignOff")]
    public async Task<IActionResult> GetSignOff(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/boq/sign-off")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.BoqSignOffs.FirstOrDefaultAsync(s => s.ProjectId == projectId));

    [Function("RecordBoqSignOff")]
    public async Task<IActionResult> RecordSignOff(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "boq/sign-offs")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<BoqSignOffEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.BoqSignOffs.FirstOrDefaultAsync(s => s.ProjectId == incoming.ProjectId);
        if (existing is null) context.BoqSignOffs.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }
}
