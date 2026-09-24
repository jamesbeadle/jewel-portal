using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// One version of a skill or of a reference document under it, as it stood while it was in force.
/// <c>WrittenAt</c> is null only for a first version saved before 2026-09-24, when the store did not
/// keep it; <c>ReplacedAt</c> is null for the version in force now. <c>Pinned</c> and
/// <c>IsActive</c> are a skill's flags as that version had them — null on a reference, and on a
/// skill revision kept before the flags were recorded.
/// </summary>
public sealed record SkillVersion(
    int Version,
    string DisplayName,
    string Description,
    string Body,
    string WrittenByEmail,
    DateTimeOffset? WrittenAt,
    DateTimeOffset? ReplacedAt,
    bool? Pinned,
    bool? IsActive)
{
    public bool IsCurrent => ReplacedAt is null;

    public int Characters => Body.Length;
}

/// <summary>Every version of one reference document, newest first.</summary>
public sealed record SkillReferenceHistory(
    string RefKey,
    string DisplayName,
    IReadOnlyList<SkillVersion> Versions);

/// <summary>Every version of a skill and of each of its reference documents, newest first.</summary>
public sealed record SkillHistory(
    string SkillKey,
    string DisplayName,
    IReadOnlyList<SkillVersion> Versions,
    IReadOnlyList<SkillReferenceHistory> References);

/// <summary>The AI Skills page's History panel and the connector's list_skill_history.</summary>
public sealed record GetAiSkillHistory(string SkillKey) : IQuery<SkillHistory?>;

/// <summary>
/// Bring an earlier version back: its name, description and text are saved as a NEW version, so
/// the version being replaced stays in the history and the restore can itself be undone. A skill
/// keeps its discipline, pin and active flag as they are now. <c>RefKey</c> names a reference
/// document under the skill; null restores the skill itself.
/// </summary>
public sealed record RestoreAiSkillVersion(
    string SkillKey,
    string? RefKey,
    int Version,
    string RestoredByEmail) : ICommand<Acknowledgement>;
