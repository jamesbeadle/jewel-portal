using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>A stored row read as the version it is — the one in force, or one a save replaced.</summary>
internal static class SkillHistoryVersions
{
    public static SkillVersion Current(SkillEntity skill) => new(
        skill.Version, skill.DisplayName, skill.Description, skill.Body,
        skill.UpdatedByEmail, skill.UpdatedAt, null, skill.Pinned, skill.IsActive);

    public static SkillVersion Replaced(SkillRevisionEntity revision) => new(
        revision.Version, revision.DisplayName, revision.Description, revision.Body,
        revision.SavedByEmail, revision.WrittenAt, revision.SavedAt, revision.IsPinned, revision.IsActive);

    public static SkillVersion Current(SkillReferenceEntity reference) => new(
        reference.Version, reference.DisplayName, reference.Description, reference.Body,
        reference.UpdatedByEmail, reference.UpdatedAt, null, null, null);

    public static SkillVersion Replaced(SkillReferenceRevisionEntity revision) => new(
        revision.Version, revision.DisplayName, revision.Description, revision.Body,
        revision.WrittenByEmail, revision.WrittenAt, revision.ReplacedAt, null, null);
}
