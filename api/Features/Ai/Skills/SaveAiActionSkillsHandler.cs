using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>
/// Replace one target's attached-skill set — the admin page's checkbox picker saved whole. Rows
/// already matching the new set are kept (their AttachedBy trail stays honest); missing ones are
/// added; unticked ones go. In force on the very next describe_action: nothing caches attachments.
/// </summary>
public sealed class SaveAiActionSkillsHandler : ICommandHandler<SaveAiActionSkills, Acknowledgement>
{
    private readonly JpmsContext context;

    public SaveAiActionSkillsHandler(JpmsContext context) => this.context = context;

    public async Task<Acknowledgement> HandleAsync(SaveAiActionSkills command, CancellationToken cancellationToken)
    {
        var kind = command.TargetKind.Trim().ToLowerInvariant();
        var key = command.TargetKey.Trim();
        var wanted = command.SkillKeys
            .Select(skillKey => skillKey.Trim())
            .Where(skillKey => skillKey.Length > 0)
            .Distinct()
            .ToList();

        var existing = await context.AiActionSkills
            .Where(row => row.TargetKind == kind && row.TargetKey == key)
            .ToListAsync(cancellationToken);

        foreach (var row in existing.Where(row => !wanted.Contains(row.SkillKey)))
            context.AiActionSkills.Remove(row);

        var alreadyAttached = existing.Select(row => row.SkillKey).ToHashSet();
        foreach (var skillKey in wanted.Where(skillKey => !alreadyAttached.Contains(skillKey)))
        {
            context.AiActionSkills.Add(new AiActionSkillEntity
            {
                ActionSkillId = Guid.NewGuid().ToString("N"),
                TargetKind = kind,
                TargetKey = key,
                SkillKey = skillKey,
                AttachedByEmail = command.SavedByEmail,
                AttachedAt = DateTimeOffset.UtcNow
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(key);
    }
}
