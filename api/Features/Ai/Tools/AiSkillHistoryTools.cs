using Jewel.JPMS.Contracts.Ai;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The AI Skills page's History panel over the connector (2026-09-24, Nigel: "we may need to audit
/// stuff", then "ensure he can revert back to a skill easily too"). Same audience as the page.
/// </summary>
internal static partial class AiSkillHistoryTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public static IReadOnlyList<AiTool> Build() => new List<AiTool> { ListSkillHistory(), RestoreSkillVersion() };

    private static AiTool ListSkillHistory() => new(
        "list_skill_history",
        "Every version of one of the portal's stored skills and of each of its reference documents — "
        + "who wrote each version, when, and when it was replaced — the AI Skills page's History panel. "
        + "Without version or as_of it lists the versions; with version (a number) or as_of (a date or "
        + "date-time — a date alone means the end of that day) it returns that version's full text, so "
        + "\"what did the skill say on the 12th\" is one call. Pass ref_key for a reference document.",
        AiToolSchema.Object(
            ("skill_key", "string", "The skill's key, as list_skills gives it.", true),
            ("ref_key", "string", "A reference document under the skill; leave out for the skill itself.", false),
            ("version", "integer", "Return this version's text.", false),
            ("as_of", "string", "Return the text in force at this date or date-time.", false)),
        AiToolKind.Read,
        SkillRoles.ManageSkills,
        async (context, input, ct) =>
        {
            var skillKey = AiToolSchema.Text(input, "skill_key")?.Trim();
            if (string.IsNullOrWhiteSpace(skillKey)) return Fail("A skill_key is required.");

            var history = await context.Services
                .GetRequiredService<IQueryHandler<GetAiSkillHistory, SkillHistory?>>()
                .HandleAsync(new GetAiSkillHistory(skillKey), ct);
            if (history is null) return Fail($"No skill named {skillKey} exists — list_skills shows the keys that do.");

            var refKey = AiToolSchema.Text(input, "ref_key")?.Trim();
            var versions = VersionsOf(history, refKey);
            if (versions is null) return Fail($"The skill {skillKey} has no reference named {refKey}.");

            var number = AiToolSchema.Number(input, "version");
            var asOfText = AiToolSchema.Text(input, "as_of");
            if (number is null && string.IsNullOrWhiteSpace(asOfText)) return Listing(history, refKey, versions);
            return OneVersion(history.SkillKey, refKey, versions, number, asOfText);
        });

    private static IReadOnlyList<SkillVersion>? VersionsOf(SkillHistory history, string? refKey) =>
        string.IsNullOrEmpty(refKey)
            ? history.Versions
            : history.References.FirstOrDefault(reference => reference.RefKey == refKey)?.Versions;

    private static string Listing(SkillHistory history, string? refKey, IReadOnlyList<SkillVersion> versions) =>
        Serialise(new
        {
            ok = true,
            skill = history.SkillKey,
            refKey,
            versions = versions.Select(Summary),
            references = string.IsNullOrEmpty(refKey)
                ? history.References.Select(reference => new { reference.RefKey, name = reference.DisplayName, versions = reference.Versions.Select(Summary) })
                : null,
            note = "Newest first. version or as_of returns a version's text; restore_skill_version brings an "
                   + "earlier one back as a new version, with the user's yes."
        });

    private static string OneVersion(
        string skillKey, string? refKey, IReadOnlyList<SkillVersion> versions, int? number, string? asOfText)
    {
        if (number is { } asked)
            return Found(skillKey, refKey, SkillVersionTimeline.Numbered(versions, asked), $"There is no version {asked}.");

        if (MomentOf(asOfText!) is not { } moment) return Fail($"as_of \"{asOfText}\" is not a date or date-time.");
        return Found(skillKey, refKey, SkillVersionTimeline.InForceAt(versions, moment),
            $"Nothing was in force at {moment:yyyy-MM-dd HH:mm} UTC — the document did not exist yet.");
    }

    private static string Found(string skillKey, string? refKey, SkillVersion? version, string missing) =>
        version is null
            ? Fail(missing)
            : Serialise(new { ok = true, skill = skillKey, refKey, version = Summary(version), version.Description, version.Body });
}
