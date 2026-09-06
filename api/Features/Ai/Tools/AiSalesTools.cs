using Jewel.JPMS.Api.Features.Sales;
using Microsoft.Extensions.DependencyInjection;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The Sales section's reads over the connector (2026-09-06): the lead register, one lead with
/// its timeline, the strategies with their funnels, one strategy with its leads. Visible to
/// every internal role, mirroring SalesRoles.Readers on the endpoints. The writes are actions:
/// capture_lead, update_lead, move_lead_stage, win_lead, log_lead_activity,
/// create_sales_strategy, update_sales_strategy, set_sales_strategy_status,
/// generate_strategy_plan (SalesActions).
/// </summary>
internal static class AiSalesTools
{
    public const string ListLeads = "list_leads";
    public const string GetLead = "get_lead";
    public const string ListSalesStrategies = "list_sales_strategies";
    public const string GetSalesStrategy = "get_sales_strategy";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };
    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public static IReadOnlyList<AiTool> Build() => new AiTool[]
    {
        new(
            ListLeads,
            "The Sales lead register, newest-captured first: every lead (LD-####) with its stage "
            + "on the ladder New → Contacted → Engaged → SiteVisit → Proposal, ending Won / Lost, "
            + "or parked in Nurture; who (contact, company, prospect kind), where (property "
            + "address, postcode), what (summary), how it was found (source, and the strategy "
            + "when one found it), estimated value, owner, dates, and for Won leads the client "
            + "and project it became. Filter with stage and/or strategyId. Call this for anything "
            + "about who is in the pipeline, what a strategy has found, or which leads are open.",
            AiToolSchema.Object(
                ("stage", "string", "Only leads at this stage: New, Contacted, Engaged, SiteVisit, Proposal, Won, Lost, Nurture — or 'open' for every open stage.", false),
                ("strategyId", "string", "Only leads found by this strategy (list_sales_strategies).", false)),
            AiToolKind.Read,
            SalesRoles.Readers,
            async (context, input, ct) =>
            {
                var leads = await context.Services
                    .GetRequiredService<IQueryHandler<ListLeads, IReadOnlyList<Lead>>>()
                    .HandleAsync(new ListLeads(), ct);
                var stageText = AiToolSchema.Text(input, "stage");
                var strategyId = AiToolSchema.Text(input, "strategyId");
                IEnumerable<Lead> rows = leads;
                if (!string.IsNullOrWhiteSpace(strategyId))
                    rows = rows.Where(lead => string.Equals(lead.StrategyId, strategyId, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(stageText))
                {
                    if (string.Equals(stageText, "open", StringComparison.OrdinalIgnoreCase))
                        rows = rows.Where(lead => lead.Stage.IsOpen());
                    else if (Enum.TryParse<LeadStage>(stageText.Replace(" ", ""), true, out var stage))
                        rows = rows.Where(lead => lead.Stage == stage);
                    else
                        return Fail($"Unknown stage \"{stageText}\" — use New, Contacted, Engaged, SiteVisit, Proposal, Won, Lost, Nurture or open.");
                }
                var list = rows.ToList();
                return Serialise(new
                {
                    ok = true,
                    count = list.Count,
                    ladder = LeadStageExtensions.Ladder.Select(stage => stage.ToString()),
                    leads = list.Select(LeadRow)
                });
            }),

        new(
            GetLead,
            "One lead in full with its timeline — every touch (calls, emails, letters, meetings, "
            + "site visits, proposals, notes) and every stage change, newest first. Takes the "
            + "leadId from list_leads, or an LD-#### reference.",
            AiToolSchema.Object(
                ("leadId", "string", "The lead's id (list_leads) or its LD-#### reference.", true)),
            AiToolKind.Read,
            SalesRoles.Readers,
            async (context, input, ct) =>
            {
                var key = AiToolSchema.Text(input, "leadId")?.Trim();
                if (string.IsNullOrWhiteSpace(key)) return Fail("leadId is required.");
                var leadId = await ResolveLeadIdAsync(context, key, ct);
                if (leadId is null) return Fail($"No lead matches \"{key}\".");
                var detail = await context.Services
                    .GetRequiredService<IQueryHandler<GetLead, LeadDetail?>>()
                    .HandleAsync(new GetLead(leadId), ct);
                if (detail is null) return Fail($"No lead matches \"{key}\".");
                return Serialise(new
                {
                    ok = true,
                    lead = LeadRow(detail.Lead),
                    detail.Lead.Notes,
                    timeline = detail.Activities.Select(activity => new
                    {
                        activity.LeadActivityId,
                        kind = activity.Kind.ToString(),
                        activity.Summary,
                        activity.OccurredAt,
                        activity.RecordedByEmail
                    })
                });
            }),

        new(
            ListSalesStrategies,
            "Every sales strategy — a methodology for FINDING leads, written down with its "
            + "justification: audience, target area, hypothesis (why these people, why now), the "
            + "evidence behind it, channel, proposition, whether an approach plan has been "
            + "drafted, status (Active first, then Draft, Paused, Retired), owner — each with its "
            + "funnel: leads found, how many reached Contacted / Engaged / Proposal, Won, Lost, "
            + "Nurture, the open pipeline value and the won value. Call this to compare "
            + "strategies, to find a strategyId for capture_lead, or before proposing a new one.",
            AiToolSchema.Empty(),
            AiToolKind.Read,
            SalesRoles.Readers,
            async (context, _, ct) =>
            {
                var strategies = await context.Services
                    .GetRequiredService<IQueryHandler<ListSalesStrategies, IReadOnlyList<SalesStrategyOverview>>>()
                    .HandleAsync(new ListSalesStrategies(), ct);
                return Serialise(new
                {
                    ok = true,
                    count = strategies.Count,
                    strategies = strategies.Select(row => new
                    {
                        row.Strategy.StrategyId,
                        row.Strategy.Name,
                        audience = row.Strategy.Audience.ToString(),
                        row.Strategy.TargetArea,
                        row.Strategy.Hypothesis,
                        row.Strategy.Evidence,
                        channel = row.Strategy.Channel.ToString(),
                        row.Strategy.Proposition,
                        hasApproachPlan = !string.IsNullOrWhiteSpace(row.Strategy.ApproachPlan),
                        row.Strategy.PlanGeneratedAt,
                        status = row.Strategy.Status.ToString(),
                        row.Strategy.OwnerEmail,
                        row.Strategy.CreatedAt,
                        row.Strategy.UpdatedAt,
                        funnel = row.Funnel
                    })
                });
            }),

        new(
            GetSalesStrategy,
            "One sales strategy in full — its definition, the approach plan (markdown), its "
            + "funnel and every lead it has found. Takes the strategyId from list_sales_strategies.",
            AiToolSchema.Object(
                ("strategyId", "string", "The strategy's id (list_sales_strategies).", true)),
            AiToolKind.Read,
            SalesRoles.Readers,
            async (context, input, ct) =>
            {
                var strategyId = AiToolSchema.Text(input, "strategyId")?.Trim();
                if (string.IsNullOrWhiteSpace(strategyId)) return Fail("strategyId is required.");
                var detail = await context.Services
                    .GetRequiredService<IQueryHandler<GetSalesStrategy, SalesStrategyDetail?>>()
                    .HandleAsync(new GetSalesStrategy(strategyId), ct);
                if (detail is null) return Fail($"No strategy matches \"{strategyId}\".");
                return Serialise(new
                {
                    ok = true,
                    strategy = new
                    {
                        detail.Strategy.StrategyId,
                        detail.Strategy.Name,
                        audience = detail.Strategy.Audience.ToString(),
                        detail.Strategy.TargetArea,
                        detail.Strategy.Hypothesis,
                        detail.Strategy.Evidence,
                        channel = detail.Strategy.Channel.ToString(),
                        detail.Strategy.Proposition,
                        detail.Strategy.ApproachPlan,
                        detail.Strategy.PlanGeneratedAt,
                        status = detail.Strategy.Status.ToString(),
                        detail.Strategy.OwnerEmail,
                        detail.Strategy.CreatedAt,
                        detail.Strategy.UpdatedAt
                    },
                    funnel = detail.Funnel,
                    leads = detail.Leads.Select(LeadRow)
                });
            })
    };

    private static object LeadRow(Lead lead) => new
    {
        lead.LeadId,
        lead.Reference,
        lead.ContactName,
        lead.ContactEmail,
        lead.ContactPhone,
        lead.CompanyName,
        prospectKind = lead.ProspectKind.ToString(),
        lead.PropertyAddress,
        lead.Postcode,
        lead.Summary,
        source = lead.Source.ToString(),
        lead.StrategyId,
        lead.StrategyName,
        stage = lead.Stage.ToString(),
        isOpen = lead.Stage.IsOpen(),
        lead.StageChangedAt,
        lead.EstimatedValue,
        lead.OwnerEmail,
        lead.CapturedAt,
        lead.ClientId,
        lead.ProjectId,
        lead.LostReason
    };

    /// <summary>An id as given, or an LD-#### (or bare number) reference resolved through the
    /// register's Number column.</summary>
    private static async Task<string?> ResolveLeadIdAsync(AiToolContext context, string key, CancellationToken ct)
    {
        var match = System.Text.RegularExpressions.Regex.Match(key.Trim(), "^(?:LD[-\\s]?)?0*(\\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var number))
        {
            var byNumber = await context.Db.Leads.AsNoTracking()
                .Where(row => row.Number == number).Select(row => row.LeadId).FirstOrDefaultAsync(ct);
            if (byNumber is not null) return byNumber;
        }
        return await context.Db.Leads.AsNoTracking()
            .Where(row => row.LeadId == key).Select(row => row.LeadId).FirstOrDefaultAsync(ct);
    }
}
