using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class IssueValuationAuthorisation
{
    private static readonly RoleSet RolesThatMayIssueValuations = RoleSet.Of(JpmsRoles.Director);

    public bool Allows(SignedInUser user, IssueValuation command) => RolesThatMayIssueValuations.Includes(user.Role);
}
