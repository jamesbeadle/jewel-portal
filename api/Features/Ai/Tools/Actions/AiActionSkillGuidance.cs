using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// Resolves the skills the team has attached to an action — directly, or through its area — so
/// describe_action can inline their doctrine next to the argument schema (2026-08-31). Read fresh
/// from the database on every call: a skill edit or a re-wiring on the AI Actions admin page is in
/// force on the very next describe_action, no deploy, no cache.
/// </summary>
internal static class AiActionSkillGuidance
{
    public static async Task<IReadOnlyList<object>> LoadForAsync(
        JpmsContext db, AiAction action, CancellationToken cancellationToken)
    {
        // Attachments written before the 2026-09-03 Drawings → Documents rename still carry the
        // old action / area key; AiLegacyNames lists every spelling the entry has had.
        var actionKeys = AiLegacyNames.AllNamesFor(action.Name);
        var areaKeys = AiLegacyNames.AllNamesFor(action.Area);
        var skillKeys = await db.AiActionSkills
            .AsNoTracking()
            .Where(row =>
                (row.TargetKind == AiActionSkillTargets.Action && actionKeys.Contains(row.TargetKey))
                || (row.TargetKind == AiActionSkillTargets.Area && areaKeys.Contains(row.TargetKey)))
            .Select(row => row.SkillKey)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (skillKeys.Count == 0) return Array.Empty<object>();

        var skills = await db.Skills
            .AsNoTracking()
            .Where(row => skillKeys.Contains(row.SkillKey) && row.IsActive)
            .OrderBy(row => row.SkillKey)
            .ToListAsync(cancellationToken);
        if (skills.Count == 0) return Array.Empty<object>();

        var loadedKeys = skills.Select(row => row.SkillKey).ToList();
        var references = await db.SkillReferences
            .AsNoTracking()
            .Where(row => loadedKeys.Contains(row.SkillKey))
            .OrderBy(row => row.RefKey)
            .Select(row => new { row.SkillKey, row.RefKey, row.DisplayName, row.Description })
            .ToListAsync(cancellationToken);

        return skills
            .Select(skill => (object)new
            {
                skill = skill.SkillKey,
                name = skill.DisplayName,
                version = skill.Version,
                body = skill.Body,
                references = references
                    .Where(reference => reference.SkillKey == skill.SkillKey)
                    .Select(reference => new
                    {
                        refKey = reference.RefKey,
                        name = reference.DisplayName,
                        description = reference.Description
                    })
                    .ToList()
            })
            .ToList();
    }

    /// <summary>Every action name and area that carries at least one ACTIVE attached skill — what
    /// list_actions uses to mark guidance without loading any bodies.</summary>
    public static async Task<HashSet<string>> TargetsWithGuidanceAsync(
        JpmsContext db, CancellationToken cancellationToken)
    {
        var targets = await db.AiActionSkills
            .AsNoTracking()
            .Where(row => db.Skills.Any(skill => skill.SkillKey == row.SkillKey && skill.IsActive))
            .Select(row => row.TargetKey)
            .Distinct()
            .ToListAsync(cancellationToken);
        // Keys written before a rename (AiLegacyNames) count for their current entry too.
        return targets.Select(AiLegacyNames.Current).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
