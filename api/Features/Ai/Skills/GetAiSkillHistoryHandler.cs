using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

public sealed class GetAiSkillHistoryHandler : IQueryHandler<GetAiSkillHistory, SkillHistory?>
{
    private readonly JpmsContext context;

    public GetAiSkillHistoryHandler(JpmsContext context) => this.context = context;

    public async Task<SkillHistory?> HandleAsync(GetAiSkillHistory query, CancellationToken cancellationToken)
    {
        var key = query.SkillKey.Trim();
        var skill = await context.Skills.AsNoTracking()
            .FirstOrDefaultAsync(row => row.SkillKey == key, cancellationToken);
        if (skill is null) return null;

        var revisions = await context.SkillRevisions.AsNoTracking()
            .Where(row => row.SkillKey == key).ToListAsync(cancellationToken);
        var references = await context.SkillReferences.AsNoTracking()
            .Where(row => row.SkillKey == key).OrderBy(row => row.RefKey).ToListAsync(cancellationToken);
        var referenceRevisions = await context.SkillReferenceRevisions.AsNoTracking()
            .Where(row => row.SkillKey == key).ToListAsync(cancellationToken);

        var versions = revisions.Select(revision => SkillHistoryVersions.Replaced(revision)).Append(SkillHistoryVersions.Current(skill));
        return new SkillHistory(
            skill.SkillKey,
            skill.DisplayName,
            SkillVersionTimeline.Of(versions),
            references.Select(reference => HistoryOf(reference, referenceRevisions)).ToList());
    }

    private static SkillReferenceHistory HistoryOf(
        SkillReferenceEntity reference, IEnumerable<SkillReferenceRevisionEntity> revisions)
    {
        var versions = revisions
            .Where(revision => revision.RefKey == reference.RefKey)
            .Select(revision => SkillHistoryVersions.Replaced(revision))
            .Append(SkillHistoryVersions.Current(reference));
        return new SkillReferenceHistory(reference.RefKey, reference.DisplayName, SkillVersionTimeline.Of(versions));
    }
}
