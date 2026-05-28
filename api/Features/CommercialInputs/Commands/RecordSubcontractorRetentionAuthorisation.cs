using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CommercialInputs;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Commands;

public sealed class RecordSubcontractorRetentionAuthorisation
{
    private static readonly RoleSet RolesThatMayRecordRetention =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, RecordSubcontractorRetention command) =>
        RolesThatMayRecordRetention.IncludesAny(user.Roles);
}
