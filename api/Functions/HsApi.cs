using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class HsApi
{
    private readonly JpmsContext context;

    public HsApi(JpmsContext context) { this.context = context; }

    [Function("ListHsRecords")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hs-records")] HttpRequest request) =>
        new OkObjectResult(await context.HsRecords.OrderByDescending(r => r.RaisedAt).ToListAsync());

    [Function("UpsertHsRecord")]
    public async Task<IActionResult> Upsert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "hs-records")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<HsRecordEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.HsRecords.FindAsync(incoming.HsRecordId);
        if (existing is null) context.HsRecords.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }

    [Function("ListAttendance")]
    public async Task<IActionResult> ListAttendance(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hs-records/{hsRecordId}/attendance")] HttpRequest request,
        string hsRecordId) =>
        new OkObjectResult(await context.HsRecordAttendance.Where(a => a.HsRecordId == hsRecordId).ToListAsync());

    [Function("AddAttendance")]
    public async Task<IActionResult> AddAttendance(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hs-records/attendance")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<HsRecordAttendanceEntity>();
        if (incoming is null) return new BadRequestResult();
        context.HsRecordAttendance.Add(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }
}
