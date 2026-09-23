using Jewel.JPMS.Api.Features.Forms;

namespace Jewel.JPMS.Api.Features.Hs.Notifications;

/// <summary>What one project's digest says around its events: the project's name, each corrective action's words, how many are still open, and the link to the H&amp;S tab.</summary>
internal static class HsDigestContexts
{
    public static async Task<HsDigestContext> ReadAsync(JpmsContext context, FormSiteOptions options, string projectId, CancellationToken cancellationToken)
    {
        var projectName = await context.Projects.AsNoTracking()
            .Where(project => project.ProjectId == projectId).Select(project => project.Name).FirstOrDefaultAsync(cancellationToken) ?? projectId;
        var actions = await context.HsRecords.AsNoTracking()
            .Where(record => record.ProjectId == projectId && record.Kind == (int)HsRecordKind.CorrectiveAction)
            .Select(record => new { record.HsRecordId, record.Summary, record.Status })
            .ToListAsync(cancellationToken);
        var summaries = actions.ToDictionary(action => action.HsRecordId, action => action.Summary);
        var openCount = actions.Count(action => action.Status != (int)HsStatus.Closed);
        var link = $"{options.PublicSiteUrl.TrimEnd('/')}/projects/{projectId}/hs?view=actions";
        return new HsDigestContext(projectName, summaries, openCount, link);
    }
}
