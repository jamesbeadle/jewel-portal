using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>Upsert, versioned exactly as a skill is: an existing reference's outgoing text is
/// copied to SkillReferenceRevisions before the new text replaces it.</summary>
public sealed class SaveAiSkillReferenceHandler : ICommandHandler<SaveAiSkillReference, Acknowledgement>
{
    private readonly JpmsContext context;

    public SaveAiSkillReferenceHandler(JpmsContext context) => this.context = context;

    public async Task<Acknowledgement> HandleAsync(SaveAiSkillReference command, CancellationToken cancellationToken)
    {
        var skillKey = command.SkillKey.Trim();
        var refKey = command.RefKey.Trim();

        // A reference belongs to a skill that exists — filing one against a typo'd key would park
        // it somewhere load_skill never lists.
        var skillExists = await context.Skills
            .AsNoTracking()
            .AnyAsync(row => row.SkillKey == skillKey, cancellationToken);
        if (!skillExists)
            throw new InvalidOperationException($"No skill named {skillKey} exists — save the skill first.");

        var existing = await context.SkillReferences
            .FirstOrDefaultAsync(row => row.SkillKey == skillKey && row.RefKey == refKey, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            var reference = new SkillReferenceEntity
            {
                SkillReferenceId = Guid.NewGuid().ToString("N"), SkillKey = skillKey, RefKey = refKey, Version = 1
            };
            Write(reference, command, now);
            context.SkillReferences.Add(reference);
        }
        else
        {
            context.SkillReferenceRevisions.Add(RevisionOf(existing, now));
            existing.Version += 1;
            Write(existing, command, now);
        }

        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement($"{skillKey}/{refKey}");
    }

    private static SkillReferenceRevisionEntity RevisionOf(SkillReferenceEntity outgoing, DateTimeOffset replacedAt) => new()
    {
        SkillReferenceRevisionId = Guid.NewGuid().ToString("N"),
        SkillKey = outgoing.SkillKey,
        RefKey = outgoing.RefKey,
        Version = outgoing.Version,
        DisplayName = outgoing.DisplayName,
        Description = outgoing.Description,
        Body = outgoing.Body,
        WrittenByEmail = outgoing.UpdatedByEmail,
        WrittenAt = outgoing.UpdatedAt,
        ReplacedAt = replacedAt
    };

    private static void Write(SkillReferenceEntity reference, SaveAiSkillReference command, DateTimeOffset now)
    {
        reference.DisplayName = command.DisplayName.Trim();
        reference.Description = command.Description.Trim();
        reference.Body = command.Body;
        reference.UpdatedByEmail = command.SavedByEmail;
        reference.UpdatedAt = now;
    }
}
