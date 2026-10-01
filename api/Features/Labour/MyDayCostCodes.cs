using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>The cost codes a worker may put their day against on a project: the trades the site
/// has — every code on its cost-code budgets, its bill of quantities, its valuation lines and its
/// work orders — or the whole active master list while the site has none yet, so the day can
/// always be coded (Jeremy, 30 Sep 2026: "if there was no demolition then they can't choose
/// demolition"). The day's card lists them and the sign-out is checked against them.</summary>
public sealed class MyDayCostCodes
{
    private readonly JpmsContext context;
    public MyDayCostCodes(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlySet<string>> AllowedForAsync(string projectId, CancellationToken cancellationToken)
    {
        var byProject = await ByProjectAsync(new[] { projectId }, cancellationToken);
        var codes = byProject[projectId].Select(code => code.Code);
        return codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<SiteSheetCostCode>>> ByProjectAsync(
        IReadOnlyList<string> projectIds, CancellationToken cancellationToken)
    {
        var sitesHave = await CodesTheSitesHaveAsync(projectIds, cancellationToken);
        var centres = await context.CostCenters
            .Where(centre => centre.IsActive)
            .OrderBy(centre => centre.SortOrder)
            .Select(centre => new SiteSheetCostCode(centre.Code, centre.Name))
            .ToListAsync(cancellationToken);

        var byProject = new Dictionary<string, IReadOnlyList<SiteSheetCostCode>>();
        foreach (var projectId in projectIds)
        {
            var siteHas = sitesHave.Where(row => row.ProjectId == projectId).Select(row => row.CostCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var hasTrades = siteHas.Count > 0;
            byProject[projectId] = hasTrades ? centres.Where(centre => siteHas.Contains(centre.Code)).ToList() : centres;
        }
        return byProject;
    }

    /// <summary>Every (project, cost code) the sites carry, from the four places a project's trades are written down.</summary>
    private async Task<IReadOnlyList<SiteCode>> CodesTheSitesHaveAsync(IReadOnlyList<string> projectIds, CancellationToken cancellationToken)
    {
        var budgeted = await context.CostCodeBudgets
            .Where(budget => projectIds.Contains(budget.ProjectId))
            .Select(budget => new SiteCode(budget.ProjectId, budget.CostCode)).ToListAsync(cancellationToken);
        var billed = await context.BoqLineItems
            .Where(line => projectIds.Contains(line.ProjectId))
            .Select(line => new SiteCode(line.ProjectId, line.CostCode)).ToListAsync(cancellationToken);
        var valued = await context.ValuationLineItems
            .Where(line => projectIds.Contains(line.ProjectId))
            .Select(line => new SiteCode(line.ProjectId, line.CostCode)).ToListAsync(cancellationToken);
        var ordered = await context.WorkOrderLines
            .Join(context.WorkOrders, line => line.WorkOrderId, order => order.WorkOrderId, (line, order) => new { order.ProjectId, line.CostCode })
            .Where(row => projectIds.Contains(row.ProjectId))
            .Select(row => new SiteCode(row.ProjectId, row.CostCode)).ToListAsync(cancellationToken);
        var written = new List<SiteCode>();
        written.AddRange(budgeted); written.AddRange(billed); written.AddRange(valued); written.AddRange(ordered);
        return written.Where(row => row.CostCode.Length > 0).Distinct().ToList();
    }

    private sealed record SiteCode(string ProjectId, string CostCode);
}
