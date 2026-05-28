using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CommercialInputs;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Commands;

public sealed class RecordContraChargeAuthorisation
{
    private static readonly RoleSet RolesThatMayRecordContraCharges =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, RecordContraCharge command) =>
        RolesThatMayRecordContraCharges.IncludesAny(user.Roles);
}
