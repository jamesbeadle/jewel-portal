namespace Jewel.JPMS.Api.Features.Ai.Tools;

// find_by_reference's Sales stems (2026-09-15): LD-#### is a lead, EST-#### an estimate on one.
// Both are global sequences, so each number names at most one record; a lead belongs to no
// project, so neither match carries one.
public static partial class AiToolCatalogue
{
    private static async Task<string?> FindLeadByNumberAsync(AiToolContext context, int number, CancellationToken ct)
    {
        var lead = await context.Db.Leads.AsNoTracking().FirstOrDefaultAsync(row => row.Number == number, ct);
        if (lead is null) return null;
        return Serialise(new
        {
            ok = true,
            kind = "lead",
            matches = new[]
            {
                new
                {
                    reference = lead.DisplayReference,
                    lead.LeadId,
                    lead.ContactName,
                    lead.CompanyName,
                    propertyAddress = lead.SiteAddress,
                    lead.Postcode,
                    lead.Summary,
                    stage = ((LeadStage)lead.Stage).ToString(),
                    lead.OwnerEmail,
                    lead.ProjectId,
                    route = $"/sales/leads/{lead.LeadId}"
                }
            },
            note = "get_lead reads the lead with its timeline and estimates; read_record_emails "
                + "record_type lead reads the enquiry mail tagged to it; create_estimate opens an "
                + "estimate on it (leadId)."
        });
    }

    private static async Task<string?> FindEstimateByNumberAsync(AiToolContext context, int number, CancellationToken ct)
    {
        var estimate = await context.Db.LeadEstimates.AsNoTracking().FirstOrDefaultAsync(row => row.Number == number, ct);
        if (estimate is null) return null;
        var lead = await context.Db.Leads.AsNoTracking().FirstOrDefaultAsync(row => row.LeadId == estimate.LeadId, ct);
        return Serialise(new
        {
            ok = true,
            kind = "estimate",
            matches = new[]
            {
                new
                {
                    reference = estimate.Reference,
                    estimate.EstimateId,
                    estimate.LeadId,
                    lead = lead?.DisplayReference,
                    leadContact = lead?.ContactName,
                    estimate.Scope,
                    estimate.ArchitectName,
                    estimate.PriceDueOn,
                    estimate.BudgetMentioned,
                    estimate.Total,
                    status = ((EstimateStatus)estimate.Status).ToString(),
                    estimate.StatusChangedAt,
                    estimate.SubmittedAt,
                    route = lead is null ? null : $"/sales/leads/{lead.LeadId}"
                }
            },
            note = "update_estimate_details and move_estimate_status take this estimateId — never "
                + "the reference. get_lead (leadId) reads the lead it sits on."
        });
    }
}
