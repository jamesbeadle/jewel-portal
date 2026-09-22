using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Features.Hs.Home;

/// <summary>
/// What the H&S officer's home shows, derived from the two cross-project reads: the open
/// corrective actions per live site (overdue first, then soonest due) and each live site's
/// standing — its last audit, and a draft still waiting for her Issue. Pure: the panel and the
/// count tiles both read it, so they can never disagree.
/// </summary>
public static class HsOfficerOverview
{
    public static IReadOnlyList<HsRecord> OpenActions(IReadOnlyList<HsRecord> records, IReadOnlyList<Project> liveProjects)
    {
        var liveProjectIds = liveProjects.Select(project => project.ProjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return records
            .Where(record => record.Kind == HsRecordKind.CorrectiveAction && record.IsOpen())
            .Where(record => liveProjectIds.Contains(record.ProjectId))
            .OrderByDescending(record => record.IsOverdue())
            .ThenBy(record => record.DueAt ?? DateTimeOffset.MaxValue)
            .ThenByDescending(record => record.RaisedAt)
            .ToList();
    }

    public static IReadOnlyList<HsSiteStanding> SiteStandings(IReadOnlyList<HsAudit> audits, IReadOnlyList<Project> liveProjects)
    {
        var auditsByProject = audits.ToLookup(audit => audit.ProjectId, StringComparer.OrdinalIgnoreCase);
        return liveProjects
            .Select(project => StandingOf(project, auditsByProject[project.ProjectId].ToList()))
            .ToList();
    }

    public static int AwaitingIssueCount(IReadOnlyList<HsSiteStanding> standings) =>
        standings.Count(standing => standing.AwaitingIssue is not null);

    private static HsSiteStanding StandingOf(Project project, IReadOnlyList<HsAudit> audits)
    {
        var lastIssued = audits
            .Where(audit => audit.Status != HsAuditStatus.Draft)
            .OrderByDescending(audit => audit.InspectionDate)
            .ThenByDescending(audit => audit.Number)
            .FirstOrDefault();
        var awaitingIssue = audits
            .Where(audit => audit.Status == HsAuditStatus.Draft)
            .OrderByDescending(audit => audit.Number)
            .FirstOrDefault();
        return new HsSiteStanding(project, lastIssued, awaitingIssue);
    }
}

/// <summary>One live site as the officer sees it: the last audit she issued there, and the
/// draft still open on it, either of which may be missing.</summary>
public sealed record HsSiteStanding(Project Project, HsAudit? LastIssued, HsAudit? AwaitingIssue);
