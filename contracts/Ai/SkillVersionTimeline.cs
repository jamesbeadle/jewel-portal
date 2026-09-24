namespace Jewel.JPMS.Contracts.Ai;

/// <summary>The reading of a document's versions as one timeline — the page, the connector and the
/// restore all read it here.</summary>
public static class SkillVersionTimeline
{
    /// <summary>Newest first. A version saved the moment the one before it was replaced, so a
    /// written time the store did not keep is read off that replacement.</summary>
    public static IReadOnlyList<SkillVersion> Of(IEnumerable<SkillVersion> versions)
    {
        var timeline = new List<SkillVersion>();
        DateTimeOffset? previousReplacedAt = null;
        foreach (var version in versions.OrderBy(version => version.Version))
        {
            timeline.Add(version with { WrittenAt = version.WrittenAt ?? previousReplacedAt });
            previousReplacedAt = version.ReplacedAt;
        }
        timeline.Reverse();
        return timeline;
    }

    /// <summary>The version in force at a moment; null when the document did not exist yet.</summary>
    public static SkillVersion? InForceAt(IReadOnlyList<SkillVersion> versions, DateTimeOffset moment) =>
        versions
            .Where(version => version.ReplacedAt is not { } replacedAt || replacedAt > moment)
            .Where(version => version.WrittenAt is not { } writtenAt || writtenAt <= moment)
            .OrderBy(version => version.Version)
            .FirstOrDefault();

    public static SkillVersion? Numbered(IReadOnlyList<SkillVersion> versions, int number) =>
        versions.FirstOrDefault(version => version.Version == number);
}
