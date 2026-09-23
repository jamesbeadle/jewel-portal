using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Hs.Notifications;

/// <summary>Who a digest is for: the site manager hearing what the officer did on his site, or the officer hearing what the site said.</summary>
public enum HsDigestAudience
{
    SiteManager = 0,
    Officer = 1
}

/// <summary>One email: everything that happened on one project, for one person, in one sitting.</summary>
public sealed record HsDigest(string ProjectId, HsDigestAudience Audience, string To, IReadOnlyList<HsRecordEventEntity> Events);

/// <summary>What the planner knows about a project: whose site it is, and who the officers are.</summary>
public sealed record HsDigestRecipients(string SiteManagerEmail, IReadOnlyList<string> OfficerEmails);

/// <summary>
/// The one rule of the digest (Katy-Louise, 15 Sep 2026: "a notification to the site managers
/// once I have been on and signed off a few things in a session rather than one per item", and
/// her own notification when a site manager writes). A project's unsent events are a sitting until
/// the newest is SessionEnd old; then ONE email goes to the site manager with everything that was
/// not his own doing, and ONE to each officer with everything that was not an officer's doing.
/// Nobody is told about their own changes, and an event nobody needs to hear is spent all the same.
/// </summary>
public static class HsNotificationDigests
{
    public static readonly TimeSpan SessionEnd = TimeSpan.FromMinutes(30);

    public static IReadOnlyList<HsDigest> Plan(
        IReadOnlyList<HsRecordEventEntity> unsentEvents, IReadOnlyDictionary<string, HsDigestRecipients> recipientsByProject, DateTimeOffset now)
    {
        var settled = unsentEvents.GroupBy(occurrence => occurrence.ProjectId).Where(project => IsSittingOver(project, now));
        return settled.SelectMany(project => DigestsFor(project.Key, project.ToList(), recipientsByProject)).ToList();
    }

    public static IEnumerable<string> SettledProjectIds(IReadOnlyList<HsRecordEventEntity> unsentEvents, DateTimeOffset now) =>
        unsentEvents.GroupBy(occurrence => occurrence.ProjectId).Where(project => IsSittingOver(project, now)).Select(project => project.Key);

    private static bool IsSittingOver(IGrouping<string, HsRecordEventEntity> project, DateTimeOffset now) =>
        project.Max(occurrence => occurrence.OccurredAt) <= now - SessionEnd;

    private static IEnumerable<HsDigest> DigestsFor(
        string projectId, IReadOnlyList<HsRecordEventEntity> events, IReadOnlyDictionary<string, HsDigestRecipients> recipientsByProject)
    {
        var isKnown = recipientsByProject.TryGetValue(projectId, out var recipients);
        if (!isKnown) yield break;
        var siteManager = recipients!.SiteManagerEmail.Trim();
        var forTheSiteManager = events.Where(occurrence => !IsBy(occurrence, siteManager)).OrderBy(occurrence => occurrence.OccurredAt).ToList();
        if (siteManager.Length > 0 && forTheSiteManager.Count > 0)
            yield return new HsDigest(projectId, HsDigestAudience.SiteManager, siteManager, forTheSiteManager);
        var forTheOfficers = events.Where(occurrence => !recipients.OfficerEmails.Any(officer => IsBy(occurrence, officer)))
            .OrderBy(occurrence => occurrence.OccurredAt).ToList();
        if (forTheOfficers.Count == 0) yield break;
        foreach (var officer in recipients.OfficerEmails.Where(officer => officer.Trim().Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            yield return new HsDigest(projectId, HsDigestAudience.Officer, officer, forTheOfficers);
    }

    private static bool IsBy(HsRecordEventEntity occurrence, string email) =>
        email.Length > 0 && string.Equals(occurrence.ByEmail, email, StringComparison.OrdinalIgnoreCase);
}
