using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Retention;

namespace Jewel.JPMS.Api.Features.Retention.Commands;

public sealed class ConfirmRetentionReleaseAuthorisation
{
    // Confirming a release records that client money moved: directors and the finance director.
    private static readonly RoleSet RolesThatMayConfirmRelease =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector);

    public bool Allows(SignedInUser user, ConfirmRetentionRelease command) =>
        RolesThatMayConfirmRelease.IncludesAny(user.Roles);
}
