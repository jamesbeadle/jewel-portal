using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class ProcurementApi
{
    private readonly JpmsContext context;

    public ProcurementApi(JpmsContext context) { this.context = context; }

    [Function("ListBidPackages")]
    public async Task<IActionResult> ListPackages(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bid-packages")] HttpRequest request) =>
        new OkObjectResult(await context.BidPackages.OrderByDescending(p => p.CreatedAt).ToListAsync());

    [Function("UpsertBidPackage")]
    public async Task<IActionResult> UpsertPackage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "bid-packages")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<BidPackageEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.BidPackages.FindAsync(incoming.BidPackageId);
        if (existing is null) context.BidPackages.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }

    [Function("ListQuotes")]
    public async Task<IActionResult> ListQuotes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bid-packages/{bidPackageId}/quotes")] HttpRequest request,
        string bidPackageId) =>
        new OkObjectResult(await context.Quotes.Where(q => q.BidPackageId == bidPackageId).ToListAsync());

    [Function("UpsertQuote")]
    public async Task<IActionResult> UpsertQuote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "quotes")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<QuoteEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.Quotes.FindAsync(incoming.QuoteId);
        if (existing is null) context.Quotes.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }

    [Function("ListWorkOrders")]
    public async Task<IActionResult> ListWorkOrders(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "work-orders")] HttpRequest request) =>
        new OkObjectResult(await context.WorkOrders.OrderByDescending(w => w.AwardedAt).ToListAsync());

    [Function("UpsertWorkOrder")]
    public async Task<IActionResult> UpsertWorkOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "work-orders")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<WorkOrderEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.WorkOrders.FindAsync(incoming.WorkOrderId);
        if (existing is null) context.WorkOrders.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }
}
