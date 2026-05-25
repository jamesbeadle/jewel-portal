using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class SubcontractorsApi
{
    private readonly JpmsContext context;

    public SubcontractorsApi(JpmsContext context) { this.context = context; }

    [Function("ListSubcontractors")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subcontractors")] HttpRequest request) =>
        new OkObjectResult(await context.Subcontractors.OrderBy(s => s.CompanyName).ToListAsync());

    [Function("UpsertSubcontractor")]
    public async Task<IActionResult> Upsert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "subcontractors")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<SubcontractorEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.Subcontractors.FindAsync(incoming.SubcontractorId);
        if (existing is null) context.Subcontractors.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }

    [Function("ListCompliance")]
    public async Task<IActionResult> ListCompliance(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subcontractors/{subcontractorId}/compliance")] HttpRequest request,
        string subcontractorId) =>
        new OkObjectResult(await context.ComplianceDocuments.Where(c => c.SubcontractorId == subcontractorId).ToListAsync());

    [Function("UpsertCompliance")]
    public async Task<IActionResult> UpsertCompliance(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subcontractors/compliance")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<ComplianceDocumentEntity>();
        if (incoming is null) return new BadRequestResult();
        var existing = await context.ComplianceDocuments.FindAsync(incoming.ComplianceDocumentId);
        if (existing is null) context.ComplianceDocuments.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }
}
