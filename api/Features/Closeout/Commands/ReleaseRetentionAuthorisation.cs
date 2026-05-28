using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class ReleaseRetentionAuthorisation
{
    private static readonly RoleSet RolesThatMayReleaseRetention = RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector);
    public bool Allows(SignedInUser user, ReleaseRetention command) => RolesThatMayReleaseRetention.IncludesAny(user.Roles);
}
