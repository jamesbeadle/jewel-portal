using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Commands;

public sealed class LogHsRecordAuthorisation
{
    private static readonly RoleSet RolesThatMayLogHsRecords =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead);

    public bool Allows(SignedInUser user, LogHsRecord command) => RolesThatMayLogHsRecords.IncludesAny(user.Roles);
}
