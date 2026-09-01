using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// The internal roles permitted to close a request — carried over unchanged from the retired agent
// framework's close-gate, where this command lived before 2026-08-26.
public sealed class CloseRequestAuthorisation
{
    private static readonly RoleSet AllowedToClose =
        RoleSet.Of(
            JpmsRoles.Director,
            JpmsRoles.ProjectManager,
            JpmsRoles.Estimator,
            JpmsRoles.SiteManager);

    public bool Allows(SignedInUser user, CloseRequest command) => AllowedToClose.IncludesAny(user.Roles);
}
