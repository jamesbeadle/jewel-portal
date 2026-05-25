using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class ChangesApi
{
    private readonly JpmsContext context;

    public ChangesApi(JpmsContext context) { this.context = context; }

    [Function("ListChanges")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "changes")] HttpRequest request) =>
        new OkObjectResult(await context.ChangeRecords.OrderByDescending(c => c.RaisedAt).ToListAsync());

    [Function("UpsertChange")]
    public async Task<IActionResult> Upsert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "changes")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<ChangeRecordEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.ChangeRecords.FindAsync(incoming.ChangeRecordId);
        if (existing is null) context.ChangeRecords.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }
}
