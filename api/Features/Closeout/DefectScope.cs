using Jewel.JPMS.Api.Features.Architects;
using Jewel.JPMS.Api.Features.ClientPortal;

namespace Jewel.JPMS.Api.Features.Closeout;

/// <summary>
/// Which project a caller may raise a defect on. The role gate answers "may this kind of user
/// raise a defect?"; this answers "on whose job?".
///
/// Role.Client is admitted so a homeowner can report a fault on their own house, and admitted for
/// ANY project id (permission check, 2026-09-19). Ownership is the client portal's own
/// ClientProjects, so both surfaces decide it the same way.
///
/// The architect is admitted by the same gate and confined the same way, to the projects that
/// name their practice (ArchitectProjects, since the login carries ArchitectId). An external
/// login with no link reaches nothing.
/// </summary>
internal static class DefectScope
{
    public static async Task<bool> IsOnTheirOwnProjectAsync(
        JpmsContext context, SignedInUser user, string projectId, CancellationToken cancellationToken)
    {
        if (JpmsRoleSets.AllInternal.IncludesAny(user.Roles)) return true;

        var architectId = ArchitectScope.OwnArchitectId(user);
        if (architectId is not null)
            return await ArchitectProjects.OwnsProjectAsync(context, architectId, projectId, cancellationToken);

        var clientId = ClientScope.OwnClientId(user);
        if (clientId is null) return false;

        return await ClientProjects.OwnsProjectAsync(context, clientId, projectId, cancellationToken);
    }
}
