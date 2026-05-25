using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class LeadsApi
{
    private readonly JpmsContext context;

    public LeadsApi(JpmsContext context) { this.context = context; }

    [Function("ListLeads")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads")] HttpRequest request) =>
        new OkObjectResult(await context.Leads.OrderByDescending(l => l.CapturedAt).ToListAsync());

    [Function("UpsertLead")]
    public async Task<IActionResult> Upsert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "leads")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<LeadEntity>();
        if (incoming is null) return new BadRequestResult();
        return await Replace(context.Leads, incoming, incoming.LeadId);
    }

    [Function("GetQualification")]
    public async Task<IActionResult> GetQualification(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads/{leadId}/qualification")] HttpRequest request,
        string leadId) =>
        new OkObjectResult(await context.QualificationAssessments.FindAsync(leadId));

    [Function("UpsertQualification")]
    public async Task<IActionResult> UpsertQualification(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leads/qualifications")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<QualificationAssessmentEntity>();
        if (incoming is null) return new BadRequestResult();
        return await Replace(context.QualificationAssessments, incoming, incoming.LeadId);
    }

    [Function("ListSiteVisits")]
    public async Task<IActionResult> ListVisits(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads/{leadId}/site-visits")] HttpRequest request,
        string leadId) =>
        new OkObjectResult(await context.SiteVisits.Where(v => v.LeadId == leadId).ToListAsync());

    [Function("UpsertSiteVisit")]
    public async Task<IActionResult> UpsertVisit(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leads/site-visits")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<SiteVisitEntity>();
        if (incoming is null) return new BadRequestResult();
        return await Replace(context.SiteVisits, incoming, incoming.SiteVisitId);
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
