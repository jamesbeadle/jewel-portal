using Jewel.JPMS.Api.Features.Architects;
using Jewel.JPMS.Api.Features.Clients;

namespace Jewel.JPMS.Api.Features.Closeout;

/// <summary>
/// Which project a caller may raise a defect on. The role gate answers "may this kind of user
/// raise a defect?"; this answers "on whose job?".
///
/// Role.Client is admitted so a homeowner can report a fault on their own house, and admitted for
/// ANY project id (permission check, 2026-09-19). Ownership is ClientProjects, the same rule every
/// read a client makes answers by (Parties/PartyReads).
///
/// The architect is admitted by the same gate and confined the same way, to the projects their
/// login was given in Admin → Users (ArchitectProjects). An external login with no link reaches
/// nothing.
/// </summary>
internal static class DefectScope
{
    public static async Task<bool> IsOnTheirOwnProjectAsync(
        JpmsContext context, SignedInUser user, string projectId, CancellationToken cancellationToken)
    {
        if (JpmsRoleSets.AllInternal.IncludesAny(user.Roles)) return true;

        var architectLogin = ArchitectScope.OwnArchitectLogin(user);
        if (architectLogin is not null)
            return await ArchitectProjects.OwnsProjectAsync(context, architectLogin, projectId, cancellationToken);

        var clientId = ClientScope.OwnClientId(user);
        if (clientId is null) return false;

        return await ClientProjects.OwnsProjectAsync(context, clientId, projectId, cancellationToken);
    }
}
