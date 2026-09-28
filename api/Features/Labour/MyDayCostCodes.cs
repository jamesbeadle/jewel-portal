using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>The cost codes a worker may put their day against on a project: the codes the project
/// has budgeted, or the whole active master list while nothing is budgeted yet, so the day can
/// always be coded. The day's card lists them and the sign-out is checked against them.</summary>
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
        var budgets = await context.CostCodeBudgets
            .Where(budget => projectIds.Contains(budget.ProjectId))
            .Select(budget => new { budget.ProjectId, budget.CostCode })
            .ToListAsync(cancellationToken);
        var centres = await context.CostCenters
            .Where(centre => centre.IsActive)
            .OrderBy(centre => centre.SortOrder)
            .Select(centre => new SiteSheetCostCode(centre.Code, centre.Name))
            .ToListAsync(cancellationToken);

        var byProject = new Dictionary<string, IReadOnlyList<SiteSheetCostCode>>();
        foreach (var projectId in projectIds)
        {
            var budgeted = budgets.Where(budget => budget.ProjectId == projectId)
                .Select(budget => budget.CostCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var hasBudget = budgeted.Count > 0;
            byProject[projectId] = hasBudget ? centres.Where(centre => budgeted.Contains(centre.Code)).ToList() : centres;
        }
        return byProject;
    }
}
