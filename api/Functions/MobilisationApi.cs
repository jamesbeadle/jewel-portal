using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class MobilisationApi
{
    private readonly JpmsContext context;

    public MobilisationApi(JpmsContext context) { this.context = context; }

    [Function("ListMobilisationItems")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/mobilisation")] HttpRequest request,
        string projectId) =>
        new OkObjectResult(await context.MobilisationItems.Where(m => m.ProjectId == projectId).ToListAsync());

    [Function("UpsertMobilisationItem")]
    public async Task<IActionResult> Upsert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "mobilisation-items")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<MobilisationItemEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.MobilisationItems.FindAsync(incoming.MobilisationItemId);
        if (existing is null) context.MobilisationItems.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }
}
