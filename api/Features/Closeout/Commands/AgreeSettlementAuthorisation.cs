using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class AgreeSettlementAuthorisation
{
    private static readonly RoleSet RolesThatMayAgreeSettlement = RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector);
    public bool Allows(SignedInUser user, AgreeSettlement command) => RolesThatMayAgreeSettlement.IncludesAny(user.Roles);
}
