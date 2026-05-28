using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class DraftValuationAuthorisation
{
    private static readonly RoleSet RolesThatMayDraftValuations =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, DraftValuation command) => RolesThatMayDraftValuations.IncludesAny(user.Roles);
}
