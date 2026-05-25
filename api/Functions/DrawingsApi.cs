using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class DrawingsApi
{
    private readonly JpmsContext context;

    public DrawingsApi(JpmsContext context) { this.context = context; }

    [Function("ListDrawings")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/drawings")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.Drawings.Where(d => d.ProjectId == projectId).OrderBy(d => d.DrawingCode).ToListAsync());

    [Function("UpsertDrawing")]
    public async Task<IActionResult> Upsert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "drawings")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<DrawingEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.Drawings.FindAsync(incoming.DrawingId);
        if (existing is null) context.Drawings.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }

    [Function("ListRevisions")]
    public async Task<IActionResult> ListRevisions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "drawings/{drawingId}/revisions")] HttpRequest request,
        string drawingId) =>
        new OkObjectResult(await context.DrawingRevisions
            .Where(r => r.DrawingId == drawingId)
            .OrderByDescending(r => r.ReceivedAt).ToListAsync());

    [Function("AddRevision")]
    public async Task<IActionResult> AddRevision(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "drawings/revisions")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<DrawingRevisionEntity>();
        if (incoming is null) return new BadRequestResult();
        context.DrawingRevisions.Add(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }
}
