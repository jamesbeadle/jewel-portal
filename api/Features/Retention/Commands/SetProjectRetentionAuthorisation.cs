using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Retention;

namespace Jewel.JPMS.Api.Features.Retention.Commands;

public sealed class SetProjectRetentionAuthorisation
{
    // Retention terms are contract terms: the directors and the finance director set them.
    private static readonly RoleSet RolesThatMaySetRetention =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector);

    public bool Allows(SignedInUser user, SetProjectRetention command) =>
        RolesThatMaySetRetention.IncludesAny(user.Roles);
}
