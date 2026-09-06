using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Sales;

internal static class SalesEntityMapping
{
    public static Lead ToModel(this LeadEntity entity, string? strategyName) =>
        new(
            entity.LeadId,
            entity.DisplayReference,
            entity.ContactName,
            entity.ContactEmail,
            entity.ContactPhone,
            entity.CompanyName,
            (LeadProspectKind)entity.ProspectKind,
            entity.SiteAddress,
            entity.Postcode,
            entity.Summary,
            entity.Notes,
            (LeadSource)entity.Source,
            entity.StrategyId,
            strategyName,
            (LeadStage)entity.Stage,
            entity.StageChangedAt,
            entity.EstimatedValue,
            entity.OwnerEmail,
            entity.CapturedAt,
            entity.ClientId,
            entity.ProjectId,
            entity.LostReason);

    public static LeadActivity ToModel(this LeadActivityEntity entity) =>
        new(entity.LeadActivityId, entity.LeadId, (LeadActivityKind)entity.Kind, entity.Summary,
            entity.OccurredAt, entity.RecordedByEmail);

    public static SalesStrategy ToModel(this SalesStrategyEntity entity) =>
        new(
            entity.StrategyId,
            entity.Name,
            (SalesAudience)entity.Audience,
            entity.TargetArea,
            entity.Hypothesis,
            entity.Evidence,
            (SalesChannel)entity.Channel,
            entity.Proposition,
            entity.ApproachPlan,
            entity.PlanGeneratedAt,
            (SalesStrategyStatus)entity.Status,
            entity.OwnerEmail,
            entity.CreatedAt,
            entity.UpdatedAt);

    /// <summary>The funnel from a strategy's leads: how many were found, how far they got, what
    /// the open ones are worth and what the won ones were.</summary>
    public static SalesStrategyFunnel ToFunnel(this IEnumerable<LeadEntity> leads)
    {
        var rows = leads.ToList();
        if (rows.Count == 0) return SalesStrategyFunnel.Empty;
        int At(LeadStage stage) => rows.Count(row => row.Stage == (int)stage);
        // "Reached" counts are cumulative up the ladder: a lead at Proposal was contacted and
        // engaged too, and a Won lead passed every open stage.
        int Reached(LeadStage stage) => rows.Count(row =>
            row.Stage == (int)LeadStage.Won || (row.Stage != (int)LeadStage.Lost && row.Stage != (int)LeadStage.Nurture && row.Stage >= (int)stage));
        return new SalesStrategyFunnel(
            Leads: rows.Count,
            Contacted: Reached(LeadStage.Contacted),
            Engaged: Reached(LeadStage.Engaged),
            Proposals: Reached(LeadStage.Proposal),
            Won: At(LeadStage.Won),
            Lost: At(LeadStage.Lost),
            Nurture: At(LeadStage.Nurture),
            PipelineValue: rows.Where(row => ((LeadStage)row.Stage).IsOpen()).Sum(row => row.EstimatedValue ?? 0m),
            WonValue: rows.Where(row => row.Stage == (int)LeadStage.Won).Sum(row => row.EstimatedValue ?? 0m));
    }

    /// <summary>Strategy names by id, for the lead lists.</summary>
    public static async Task<Dictionary<string, string>> StrategyNamesAsync(JpmsContext context, CancellationToken ct) =>
        await context.SalesStrategies.AsNoTracking()
            .Select(row => new { row.StrategyId, row.Name })
            .ToDictionaryAsync(row => row.StrategyId, row => row.Name, ct);
}
