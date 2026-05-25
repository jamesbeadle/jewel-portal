using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class LeadAttachmentsApi
{
    private readonly JpmsContext context;

    public LeadAttachmentsApi(JpmsContext context) { this.context = context; }

    [Function("ListInfoChase")]
    public async Task<IActionResult> ListInfoChase(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads/{leadId}/info-chase")] HttpRequest request,
        string leadId) =>
        new OkObjectResult(await context.InfoChaseItems.Where(i => i.LeadId == leadId).ToListAsync());

    [Function("UpsertInfoChase")]
    public async Task<IActionResult> UpsertInfoChase(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leads/info-chase")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<InfoChaseItemEntity>();
        if (incoming is null) return new BadRequestResult();
        return await ReplaceById(context.InfoChaseItems, incoming, incoming.InfoChaseItemId);
    }

    [Function("UpsertBidDecision")]
    public async Task<IActionResult> UpsertBidDecision(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leads/bid-decisions")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<BidDecisionEntity>();
        if (incoming is null) return new BadRequestResult();
        return await ReplaceById(context.BidDecisions, incoming, incoming.LeadId);
    }

    [Function("GetProposal")]
    public async Task<IActionResult> GetProposal(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads/{leadId}/proposal")] HttpRequest request,
        string leadId) =>
        new OkObjectResult(await context.Proposals.FirstOrDefaultAsync(p => p.LeadId == leadId));

    [Function("UpsertProposal")]
    public async Task<IActionResult> UpsertProposal(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leads/proposals")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<ProposalEntity>();
        if (incoming is null) return new BadRequestResult();
        return await ReplaceById(context.Proposals, incoming, incoming.ProposalId);
    }

    [Function("GetLeadOutcome")]
    public async Task<IActionResult> GetOutcome(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads/{leadId}/outcome")] HttpRequest request,
        string leadId) =>
        new OkObjectResult(await context.LeadOutcomes.FindAsync(leadId));

    [Function("UpsertLeadOutcome")]
    public async Task<IActionResult> UpsertOutcome(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leads/outcomes")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<LeadOutcomeEntity>();
        if (incoming is null) return new BadRequestResult();
        return await ReplaceById(context.LeadOutcomes, incoming, incoming.LeadId);
    }

    private async Task<IActionResult> ReplaceById<T>(DbSet<T> set, T incoming, object key) where T : class
    {
        var existing = await set.FindAsync(key);
        if (existing is null) set.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);
        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }
}
