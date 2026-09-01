using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

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
            context.SkillReferences.Add(new SkillReferenceEntity
            {
                SkillReferenceId = Guid.NewGuid().ToString("N"),
                SkillKey = skillKey,
                RefKey = refKey,
                DisplayName = command.DisplayName.Trim(),
                Description = command.Description.Trim(),
                Body = command.Body,
                UpdatedByEmail = command.SavedByEmail,
                UpdatedAt = now
            });
        }
        else
        {
            existing.DisplayName = command.DisplayName.Trim();
            existing.Description = command.Description.Trim();
            existing.Body = command.Body;
            existing.UpdatedByEmail = command.SavedByEmail;
            existing.UpdatedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement($"{skillKey}/{refKey}");
    }
}
