using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Commands;

public sealed class UpdateHsRecordAuthorisation
{
    private static readonly RoleSet RolesThatMayUpdateHsRecords =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead);

    public bool Allows(SignedInUser user, UpdateHsRecord command) => RolesThatMayUpdateHsRecords.IncludesAny(user.Roles);
}
