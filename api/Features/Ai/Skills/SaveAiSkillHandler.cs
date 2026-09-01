using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>
/// Upsert. An existing key becomes a new version with the OUTGOING body copied to SkillRevisions
/// first — a doctrine edit is never destructive, and "what did the assistant know on the 12th" is
/// answerable from the revision trail. The saved skill is live on the very next assistant turn:
/// nothing caches skill bodies.
/// </summary>
public sealed class SaveAiSkillHandler : ICommandHandler<SaveAiSkill, Acknowledgement>
{
    private readonly JpmsContext context;

    public SaveAiSkillHandler(JpmsContext context) => this.context = context;

    public async Task<Acknowledgement> HandleAsync(SaveAiSkill command, CancellationToken cancellationToken)
    {
        var key = command.SkillKey.Trim();
        var now = DateTimeOffset.UtcNow;

        var existing = await context.Skills
            .FirstOrDefaultAsync(row => row.SkillKey == key, cancellationToken);

        if (existing is null)
        {
            context.Skills.Add(new SkillEntity
            {
                SkillKey = key,
                AgentKey = command.AgentKey.Trim().ToLowerInvariant(),
                DisplayName = command.DisplayName.Trim(),
                Description = command.Description.Trim(),
                Body = command.Body,
                Pinned = command.Pinned,
                IsActive = command.IsActive,
                Version = 1,
                UpdatedByEmail = command.SavedByEmail,
                UpdatedAt = now
            });
        }
        else
        {
            // The body being replaced is kept, whole. Metadata-only edits (pin, active, agent)
            // still version — cheap, and the trail stays a complete history rather than a partial one.
            context.SkillRevisions.Add(new SkillRevisionEntity
            {
                SkillRevisionId = Guid.NewGuid().ToString("N"),
                SkillKey = existing.SkillKey,
                Version = existing.Version,
                Body = existing.Body,
                Description = existing.Description,
                SavedByEmail = existing.UpdatedByEmail,
                SavedAt = now
            });

            existing.AgentKey = command.AgentKey.Trim().ToLowerInvariant();
            existing.DisplayName = command.DisplayName.Trim();
            existing.Description = command.Description.Trim();
            existing.Body = command.Body;
            existing.Pinned = command.Pinned;
            existing.IsActive = command.IsActive;
            existing.Version += 1;
            existing.UpdatedByEmail = command.SavedByEmail;
            existing.UpdatedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(key);
    }
}
