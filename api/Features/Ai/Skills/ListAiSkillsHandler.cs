using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

public sealed class ListAiSkillsHandler : IQueryHandler<ListAiSkills, IReadOnlyList<SkillSummary>>
{
    private readonly JpmsContext context;

    public ListAiSkillsHandler(JpmsContext context) => this.context = context;

    public async Task<IReadOnlyList<SkillSummary>> HandleAsync(ListAiSkills query, CancellationToken cancellationToken)
    {
        var referenceCounts = await context.SkillReferences
            .AsNoTracking()
            .GroupBy(row => row.SkillKey)
            .Select(group => new { SkillKey = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.SkillKey, group => group.Count, cancellationToken);

        var skills = await context.Skills
            .AsNoTracking()
            .OrderBy(row => row.AgentKey)
            .ThenBy(row => row.DisplayName)
            .Select(row => new
            {
                row.SkillKey, row.AgentKey, row.DisplayName, row.Description,
                row.Pinned, row.IsActive, row.Version, BodyLength = row.Body.Length,
                row.UpdatedByEmail, row.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return skills
            .Select(row => new SkillSummary(
                row.SkillKey, row.AgentKey, row.DisplayName, row.Description,
                row.Pinned, row.IsActive, row.Version, row.BodyLength,
                referenceCounts.TryGetValue(row.SkillKey, out var count) ? count : 0,
                row.UpdatedByEmail, row.UpdatedAt))
            .ToList();
    }
}
