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
/// The architect is admitted by the same gate and is NOT scoped here: a Role.Architect login
/// carries no architect identity at all — DirectoryUserEntity has SubcontractorId and ClientId and
/// no ArchitectId — so there is nothing yet to scope them against. That is a schema decision, not
/// a fix, and the permission check goes on reporting this route until it is taken.
/// </summary>
internal static class DefectScope
{
    public static async Task<bool> IsOnTheirOwnProjectAsync(
        JpmsContext context, SignedInUser user, string projectId, CancellationToken cancellationToken)
    {
        var clientId = ClientScope.OwnClientId(user);
        if (clientId is null) return true;

        return await ClientProjects.OwnsProjectAsync(context, clientId, projectId, cancellationToken);
    }
}
