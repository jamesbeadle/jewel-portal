using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Ai;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

public sealed class GetAiSkillHandler : IQueryHandler<GetAiSkill, SkillDetail?>
{
    private readonly JpmsContext context;

    public GetAiSkillHandler(JpmsContext context) => this.context = context;

    public async Task<SkillDetail?> HandleAsync(GetAiSkill query, CancellationToken cancellationToken)
    {
        var skill = await context.Skills
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.SkillKey == query.SkillKey, cancellationToken);
        if (skill is null) return null;

        var references = await context.SkillReferences
            .AsNoTracking()
            .Where(row => row.SkillKey == skill.SkillKey)
            .OrderBy(row => row.RefKey)
            .Select(row => new SkillReferenceDetail(
                row.RefKey, row.DisplayName, row.Description, row.Body, row.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new SkillDetail(
            skill.SkillKey, skill.AgentKey, skill.DisplayName, skill.Description, skill.Body,
            skill.Pinned, skill.IsActive, skill.Version, skill.UpdatedByEmail, skill.UpdatedAt,
            references);
    }
}
