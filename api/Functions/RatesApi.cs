using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class RatesApi
{
    private readonly JpmsContext context;

    public RatesApi(JpmsContext context) { this.context = context; }

    [Function("ListRates")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rates")] HttpRequest request) =>
        new OkObjectResult(await context.Rates.OrderBy(r => r.Trade).ThenBy(r => r.Description).ToListAsync());

    [Function("UpsertRate")]
    public async Task<IActionResult> Upsert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "rates")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<RateEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.Rates.FindAsync(incoming.RateId);
        if (existing is null) context.Rates.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }
}
