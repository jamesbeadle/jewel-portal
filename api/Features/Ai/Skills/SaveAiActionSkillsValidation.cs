using Jewel.JPMS.Api.Features.Ai.Tools.Actions;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

public sealed class SaveAiActionSkillsValidation
{
    private readonly JpmsContext context;

    public SaveAiActionSkillsValidation(JpmsContext context) => this.context = context;

    public async Task<ValidationOutcome> CheckAsync(SaveAiActionSkills command, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        var kind = command.TargetKind?.Trim().ToLowerInvariant();
        if (kind is not (AiActionSkillTargets.Action or AiActionSkillTargets.Area))
            errors.Add("The target kind must be \"action\" or \"area\".");

        if (command.SkillKeys is null)
        {
            errors.Add("A skill list is required — empty detaches everything from the target.");
            return new ValidationOutcome(errors);
        }

        // ATTACHING requires a target the registry declares today — an attachment to a name
        // nothing declares would sit silently doing nothing. Removal-only saves are exempt: rows
        // orphaned by a code rename can only be cleaned up by saving a smaller set against the old
        // name, so a save that adds nothing new never needs the target to still exist.
        var key = command.TargetKey?.Trim() ?? "";
        var requestedKeys = command.SkillKeys
            .Where(skillKey => !string.IsNullOrWhiteSpace(skillKey))
            .Select(skillKey => skillKey.Trim())
            .ToList();
        var alreadyAttached = await context.AiActionSkills
            .AsNoTracking()
            .Where(row => row.TargetKind == kind && row.TargetKey == key)
            .Select(row => row.SkillKey)
            .ToListAsync(cancellationToken);
        var isAttaching = requestedKeys.Any(skillKey => !alreadyAttached.Contains(skillKey));
        if (string.IsNullOrWhiteSpace(key))
            errors.Add("A target is required — an action name or an area.");
        else if (isAttaching && kind == AiActionSkillTargets.Action && AiActionRegistry.Find(key) is null)
            errors.Add($"No action named \"{key}\" exists in the registry.");
        else if (isAttaching && kind == AiActionSkillTargets.Area && !AreaExists(key))
            errors.Add($"No area named \"{key}\" exists in the registry.");

        // Every named skill must be a real row. Inactive is allowed — retiring a skill already
        // stops it being served, and the wiring surviving means un-retiring restores it.
        var requested = requestedKeys.Distinct().ToList();
        if (requested.Count > 0)
        {
            var known = await context.Skills
                .AsNoTracking()
                .Where(row => requested.Contains(row.SkillKey))
                .Select(row => row.SkillKey)
                .ToListAsync(cancellationToken);
            foreach (var missing in requested.Except(known))
                errors.Add($"No skill named \"{missing}\" exists.");
        }

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }

    private static bool AreaExists(string area) =>
        AiActionRegistry.All.Any(action => string.Equals(action.Area, area, StringComparison.OrdinalIgnoreCase));
}
