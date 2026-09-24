using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>
/// Upsert. An existing key becomes a new version with the OUTGOING version copied to SkillRevisions
/// first — its text, name, flags, who wrote it and when — so a doctrine edit is never destructive,
/// "what did the assistant know on the 12th" is answerable from the trail, and any version can be
/// restored. The saved skill is live on the very next assistant turn: nothing caches skill bodies.
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
            var skill = new SkillEntity { SkillKey = key, Version = 1 };
            Write(skill, command, now);
            context.Skills.Add(skill);
        }
        else
        {
            // Metadata-only edits (pin, active, discipline) still version — the trail stays a
            // complete history rather than a partial one.
            context.SkillRevisions.Add(RevisionOf(existing, now));
            existing.Version += 1;
            Write(existing, command, now);
        }

        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(key);
    }

    private static SkillRevisionEntity RevisionOf(SkillEntity outgoing, DateTimeOffset replacedAt) => new()
    {
        SkillRevisionId = Guid.NewGuid().ToString("N"),
        SkillKey = outgoing.SkillKey,
        Version = outgoing.Version,
        DisplayName = outgoing.DisplayName,
        Body = outgoing.Body,
        Description = outgoing.Description,
        IsPinned = outgoing.Pinned,
        IsActive = outgoing.IsActive,
        SavedByEmail = outgoing.UpdatedByEmail,
        WrittenAt = outgoing.UpdatedAt,
        SavedAt = replacedAt
    };

    private static void Write(SkillEntity skill, SaveAiSkill command, DateTimeOffset now)
    {
        skill.AgentKey = command.AgentKey.Trim().ToLowerInvariant();
        skill.DisplayName = command.DisplayName.Trim();
        skill.Description = command.Description.Trim();
        skill.Body = command.Body;
        skill.Pinned = command.Pinned;
        skill.IsActive = command.IsActive;
        skill.UpdatedByEmail = command.SavedByEmail;
        skill.UpdatedAt = now;
    }
}
