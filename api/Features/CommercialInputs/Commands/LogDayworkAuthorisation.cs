using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CommercialInputs;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Commands;

public sealed class LogDayworkAuthorisation
{
    private static readonly RoleSet RolesThatMayLogDayworks =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.SiteManager);

    public bool Allows(SignedInUser user, LogDaywork command) =>
        RolesThatMayLogDayworks.IncludesAny(user.Roles);
}
