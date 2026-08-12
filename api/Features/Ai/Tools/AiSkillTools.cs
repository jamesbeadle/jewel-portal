using System.Text.Json;
using Jewel.JPMS.Api.Gates;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The two tools that give an agent its unpinned knowledge on demand
/// (docs/ai/05-agents-and-skills.md §2.3). Pinned skill bodies ride in the system prompt; these
/// fetch everything else — specialist skills and reference documents — only when the model decides
/// it needs them, which is what keeps a ninety-page doctrine affordable.
///
/// <para>Both replay latest-only in the transcript (see AiTranscriptBudget), so a skill loaded on
/// turn two is not paid for again on turn ten.</para>
/// </summary>
internal static class AiSkillTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    /// <summary>The agent key that marks a skill as pinned/loadable for EVERY agent.</summary>
    internal const string SharedAgentKey = "shared";

    public static IReadOnlyList<AiTool> Build()
    {
        return new List<AiTool>
        {
            new(
                "load_skill",
                "Load the full text of one of your skills — the discipline manuals listed in your "
                + "instructions under \"Your skills\". Call it when the task in hand is one a listed "
                + "skill covers and you have not already loaded it this conversation; then follow "
                + "what it says. Do not re-load a skill you already loaded — you keep what it told "
                + "you. Do not guess at keys: only the listed ones exist.",
                AiToolSchema.Object(("skill_key", "string", "A skill key from your instructions.", true)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var key = AiToolSchema.Text(input, "skill_key")?.Trim();
                    if (string.IsNullOrWhiteSpace(key)) return Fail("A skill_key is required.");

                    var skill = await context.Db.Skills
                        .AsNoTracking()
                        .FirstOrDefaultAsync(row => row.SkillKey == key && row.IsActive, ct);

                    // An agent reads its own skills and the shared set — never another agent's.
                    if (skill is null
                        || (!string.Equals(skill.AgentKey, context.AgentKey, StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(skill.AgentKey, SharedAgentKey, StringComparison.OrdinalIgnoreCase)))
                    {
                        return Fail($"No skill named {key} is available to you. Only the skills "
                                    + "listed in your instructions exist.");
                    }

                    var references = await context.Db.SkillReferences
                        .AsNoTracking()
                        .Where(row => row.SkillKey == skill.SkillKey)
                        .OrderBy(row => row.RefKey)
                        .Select(row => new { row.RefKey, row.DisplayName, row.Description })
                        .ToListAsync(ct);

                    return Serialise(new
                    {
                        ok = true,
                        skill = skill.SkillKey,
                        version = skill.Version,
                        body = skill.Body,
                        references,
                        note = references.Count == 0
                            ? "This skill has no reference documents."
                            : "Reference documents are listed by key — call load_skill_reference "
                              + "only for one the task actually needs."
                    });
                }),

            new(
                "load_skill_reference",
                "Load one reference document belonging to a skill — the larger source material "
                + "(clause maps, methodologies, precedent libraries) a skill names but does not "
                + "inline. Call load_skill first; it lists the reference keys that exist. Load a "
                + "reference only when the task actually needs its contents — they are large.",
                AiToolSchema.Object(
                    ("skill_key", "string", "The owning skill's key.", true),
                    ("ref_key", "string", "A reference key that load_skill listed.", true)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var skillKey = AiToolSchema.Text(input, "skill_key")?.Trim();
                    var refKey = AiToolSchema.Text(input, "ref_key")?.Trim();
                    if (string.IsNullOrWhiteSpace(skillKey) || string.IsNullOrWhiteSpace(refKey))
                        return Fail("Both skill_key and ref_key are required.");

                    var skill = await context.Db.Skills
                        .AsNoTracking()
                        .FirstOrDefaultAsync(row => row.SkillKey == skillKey && row.IsActive, ct);

                    if (skill is null
                        || (!string.Equals(skill.AgentKey, context.AgentKey, StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(skill.AgentKey, SharedAgentKey, StringComparison.OrdinalIgnoreCase)))
                    {
                        return Fail($"No skill named {skillKey} is available to you.");
                    }

                    var reference = await context.Db.SkillReferences
                        .AsNoTracking()
                        .FirstOrDefaultAsync(row => row.SkillKey == skillKey && row.RefKey == refKey, ct);

                    if (reference is null)
                        return Fail($"The skill {skillKey} has no reference named {refKey}. "
                                    + "load_skill lists the keys that exist.");

                    return Serialise(new
                    {
                        ok = true,
                        skill = skillKey,
                        reference = reference.RefKey,
                        title = reference.DisplayName,
                        body = reference.Body
                    });
                })
        };
    }
}
