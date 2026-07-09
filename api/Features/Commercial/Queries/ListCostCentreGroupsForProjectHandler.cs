using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListCostCentreGroupsForProjectHandler : IQueryHandler<ListCostCentreGroupsForProject, IReadOnlyList<CostCentreGroup>>
{
    private readonly JpmsContext context;

    public ListCostCentreGroupsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<CostCentreGroup>> HandleAsync(ListCostCentreGroupsForProject query, CancellationToken cancellationToken)
    {
        var groups = await context.CostCentreGroups
            .Where(group => group.ProjectId == query.ProjectId)
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);

        var members = await context.CostCentreGroupMembers
            .Where(member => member.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        var membersByGroup = members
            .GroupBy(member => member.CostCentreGroupId)
            .ToDictionary(byGroup => byGroup.Key,
                          byGroup => (IReadOnlyList<string>)byGroup
                              .Select(member => member.CostCode)
                              .OrderBy(memberCode => memberCode, StringComparer.OrdinalIgnoreCase)
                              .ToList());

        return groups
            .Select(group => new CostCentreGroup(
                group.CostCentreGroupId,
                group.ProjectId,
                group.Name,
                membersByGroup.TryGetValue(group.CostCentreGroupId, out var codes) ? codes : Array.Empty<string>()))
            .ToList();
    }
}
