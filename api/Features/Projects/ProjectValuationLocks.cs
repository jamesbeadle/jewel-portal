using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Projects;

internal static class ProjectValuationLocks
{
    public static async Task<IReadOnlyDictionary<string, DateTimeOffset>> LatestForAsync(
        JpmsContext context, IReadOnlyCollection<string> projectIds, CancellationToken cancellationToken)
    {
        var locks = await context.ValuationClaims.AsNoTracking()
            .Where(claim => projectIds.Contains(claim.ProjectId) && claim.LockedAt != null)
            .GroupBy(claim => claim.ProjectId)
            .Select(claims => new { ProjectId = claims.Key, LockedAt = claims.Max(claim => claim.LockedAt) })
            .ToListAsync(cancellationToken);
        return locks.ToDictionary(latest => latest.ProjectId, latest => latest.LockedAt!.Value);
    }

    public static async Task<DateTimeOffset?> LatestForAsync(
        JpmsContext context, string projectId, CancellationToken cancellationToken) =>
        await context.ValuationClaims.AsNoTracking()
            .Where(claim => claim.ProjectId == projectId && claim.LockedAt != null)
            .MaxAsync(claim => claim.LockedAt, cancellationToken);
}
