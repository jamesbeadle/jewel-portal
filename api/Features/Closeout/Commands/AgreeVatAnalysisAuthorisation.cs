using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class AgreeVatAnalysisAuthorisation
{
    private static readonly RoleSet RolesThatMayAgreeVat = RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector);
    public bool Allows(SignedInUser user, AgreeVatAnalysis command) => RolesThatMayAgreeVat.Includes(user.Role);
}
