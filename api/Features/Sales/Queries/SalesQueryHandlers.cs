using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Queries;

public sealed class ListLeadsHandler : IQueryHandler<ListLeads, IReadOnlyList<Lead>>
{
    private readonly JpmsContext context;
    public ListLeadsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Lead>> HandleAsync(ListLeads query, CancellationToken cancellationToken)
    {
        var names = await SalesEntityMapping.StrategyNamesAsync(context, cancellationToken);
        var rows = await context.Leads.AsNoTracking()
            .OrderByDescending(row => row.CapturedAt).ThenByDescending(row => row.Number)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToModel(row.StrategyId is null ? null : names.GetValueOrDefault(row.StrategyId))).ToList();
    }
}

public sealed class GetLeadHandler : IQueryHandler<GetLead, LeadDetail?>
{
    private readonly JpmsContext context;
    public GetLeadHandler(JpmsContext context) { this.context = context; }

    public async Task<LeadDetail?> HandleAsync(GetLead query, CancellationToken cancellationToken)
    {
        var row = await context.Leads.AsNoTracking().FirstOrDefaultAsync(lead => lead.LeadId == query.LeadId, cancellationToken);
        if (row is null) return null;
        var strategyName = row.StrategyId is null ? null
            : await context.SalesStrategies.AsNoTracking().Where(s => s.StrategyId == row.StrategyId)
                .Select(s => s.Name).FirstOrDefaultAsync(cancellationToken);
        var activities = await context.LeadActivities.AsNoTracking()
            .Where(activity => activity.LeadId == query.LeadId)
            .OrderByDescending(activity => activity.OccurredAt)
            .ToListAsync(cancellationToken);
        return new LeadDetail(row.ToModel(strategyName), activities.Select(activity => activity.ToModel()).ToList());
    }
}

public sealed class ListSalesStrategiesHandler : IQueryHandler<ListSalesStrategies, IReadOnlyList<SalesStrategyOverview>>
{
    private readonly JpmsContext context;
    public ListSalesStrategiesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<SalesStrategyOverview>> HandleAsync(ListSalesStrategies query, CancellationToken cancellationToken)
    {
        var strategies = await context.SalesStrategies.AsNoTracking().ToListAsync(cancellationToken);
        var leadsByStrategy = (await context.Leads.AsNoTracking()
                .Where(row => row.StrategyId != null)
                .ToListAsync(cancellationToken))
            .GroupBy(row => row.StrategyId!)
            .ToDictionary(group => group.Key, group => group.ToFunnel());
        // Active first (the ones being worked), then Draft, Paused, Retired; newest first within.
        static int Rank(int status) => (SalesStrategyStatus)status switch
        {
            SalesStrategyStatus.Active => 0,
            SalesStrategyStatus.Draft => 1,
            SalesStrategyStatus.Paused => 2,
            _ => 3
        };
        return strategies
            .OrderBy(row => Rank(row.Status)).ThenByDescending(row => row.CreatedAt)
            .Select(row => new SalesStrategyOverview(row.ToModel(), leadsByStrategy.GetValueOrDefault(row.StrategyId, SalesStrategyFunnel.Empty)))
            .ToList();
    }
}

public sealed class GetSalesStrategyHandler : IQueryHandler<GetSalesStrategy, SalesStrategyDetail?>
{
    private readonly JpmsContext context;
    public GetSalesStrategyHandler(JpmsContext context) { this.context = context; }

    public async Task<SalesStrategyDetail?> HandleAsync(GetSalesStrategy query, CancellationToken cancellationToken)
    {
        var strategy = await context.SalesStrategies.AsNoTracking()
            .FirstOrDefaultAsync(row => row.StrategyId == query.StrategyId, cancellationToken);
        if (strategy is null) return null;
        var leads = await context.Leads.AsNoTracking()
            .Where(row => row.StrategyId == query.StrategyId)
            .OrderByDescending(row => row.CapturedAt)
            .ToListAsync(cancellationToken);
        return new SalesStrategyDetail(
            strategy.ToModel(),
            leads.ToFunnel(),
            leads.Select(row => row.ToModel(strategy.Name)).ToList());
    }
}
