using Jewel.JPMS.Api.Features.Architects;
using Jewel.JPMS.Api.Features.Portal;

namespace Jewel.JPMS.Api.Features.Requests;

/// <summary>
/// Which request a caller may act on. The role gate answers "may this kind of user raise or edit
/// a request?"; this answers "on whose job?". The internal team works every project, so their
/// role is the whole answer. An architect is confined to the projects that name their practice
/// (ArchitectProjects); a subcontractor may raise a request only on a project they hold an issued
/// work order for (SubcontractorProjects). An external login with no link reaches nothing.
/// </summary>
internal static class RequestScope
{
    public static async Task<bool> MayRaiseOnProjectAsync(
        JpmsContext context, SignedInUser user, string projectId, CancellationToken cancellationToken)
    {
        if (JpmsRoleSets.AllInternal.IncludesAny(user.Roles)) return true;
        var architectId = ArchitectScope.OwnArchitectId(user);
        if (architectId is not null)
            return await ArchitectProjects.OwnsProjectAsync(context, architectId, projectId, cancellationToken);
        var subcontractorId = SubcontractorScope.OwnSubcontractorId(user);
        if (subcontractorId is not null)
            return await SubcontractorProjects.HoldsWorkOnAsync(context, subcontractorId, projectId, cancellationToken);
        return false;
    }

    public static async Task<bool> MayActOnAsync(
        JpmsContext context, SignedInUser user, string requestId, CancellationToken cancellationToken)
    {
        if (JpmsRoleSets.AllInternal.IncludesAny(user.Roles)) return true;
        var architectId = ArchitectScope.OwnArchitectId(user);
        if (architectId is null) return false;
        return await ArchitectProjects.OwnsRequestAsync(context, architectId, requestId, cancellationToken);
    }
}
